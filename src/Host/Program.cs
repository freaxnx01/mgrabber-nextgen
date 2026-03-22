using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using MusicGrabber.Modules.Download.Infrastructure;
using MusicGrabber.Modules.Download.Infrastructure.Adapters.Persistence;
using MusicGrabber.Modules.Discovery.Infrastructure;
using MusicGrabber.Modules.Radio.Infrastructure;
using MusicGrabber.Modules.Quota.Infrastructure;
using MusicGrabber.Modules.Quota.Infrastructure.Adapters.Persistence;
using MusicGrabber.Modules.Identity.Infrastructure;
using MusicGrabber.Modules.Identity.Infrastructure.Adapters.Persistence;
using MusicGrabber.Shared;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using MusicGrabber.Host.Endpoints;
using MusicGrabber.Host.Jobs;
using MusicGrabber.Frontend.Services;
using MusicGrabber.Shared.Events;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console());

// MudBlazor
builder.Services.AddMudServices();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Memory Cache (for Discovery & Radio)
builder.Services.AddMemoryCache();

// OpenAPI
builder.Services.AddOpenApi();

// Health Checks
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=musicgrabber.db";
builder.Services.AddHealthChecks()
    .AddSqlite(connectionString, tags: ["ready"]);

// SignalR
builder.Services.AddSignalR();

// Hangfire
builder.Services.AddHangfire(config =>
    config.UseSQLiteStorage(connectionString));
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 9;
});

// OpenTelemetry (Errata #24)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddPrometheusExporter());

// Event Bus
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();

// Modules
builder.Services.AddIdentityModule(connectionString);
builder.Services.AddQuotaModule(connectionString);
builder.Services.AddDiscoveryModule();
builder.Services.AddRadioModule();
builder.Services.AddDownloadModule(builder.Configuration);

// Frontend Services
builder.Services.AddFrontendServices();

// Google OAuth + Authentik OIDC
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? "";
        options.ClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? "";
    })
    .AddOpenIdConnect("Authentik", "Authentik", options =>
    {
        options.Authority = builder.Configuration["AUTHENTIK_AUTHORITY"] ?? "https://auth.home.freaxnx01.ch/application/o/musicgrabber/";
        options.ClientId = builder.Configuration["AUTHENTIK_CLIENT_ID"] ?? "";
        options.ClientSecret = builder.Configuration["AUTHENTIK_CLIENT_SECRET"] ?? "";
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "groups";
        options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.Email, "email");
        options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.Name, "name");
    });

// CORS
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:8080", "http://localhost:8086")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// --migrate flag (Errata #27)
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var downloadDb = scope.ServiceProvider.GetRequiredService<DownloadDbContext>();
    await downloadDb.Database.MigrateAsync();
    var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await identityDb.Database.MigrateAsync();
    var quotaDb = scope.ServiceProvider.GetRequiredService<QuotaDbContext>();
    await quotaDb.Database.MigrateAsync();
    return;
}

// Middleware
app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Health endpoints (Errata #23)
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

// OpenAPI & Scalar
app.MapOpenApi();
app.MapScalarApiReference();

// Hangfire Dashboard (Admin only)
app.MapHangfireDashboard("/hangfire");

// Prometheus metrics
app.MapPrometheusScrapingEndpoint("/metrics");

// Static files & Blazor
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<MusicGrabber.Frontend.Components.App>()
    .AddInteractiveServerRenderMode();

// API Endpoints
app.MapDownloadEndpoints();
app.MapDiscoveryEndpoints();
app.MapRadioEndpoints();
app.MapQuotaEndpoints();
app.MapIdentityEndpoints();
app.MapAdminEndpoints();

// Auth challenge endpoints
app.MapGet("/api/auth/google-login", (string? returnUrl, HttpContext ctx) =>
{
    var redirectUrl = returnUrl ?? "/";
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = redirectUrl },
        ["Google"]);
}).AllowAnonymous();

app.MapGet("/api/auth/authentik-login", (string? returnUrl, HttpContext ctx) =>
{
    var redirectUrl = returnUrl ?? "/";
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = redirectUrl },
        ["Authentik"]);
}).AllowAnonymous();

app.MapGet("/api/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync();
    return Results.Redirect("/login");
}).AllowAnonymous();

// SignalR Hub
app.MapHub<MusicGrabber.Host.Hubs.DownloadHub>("/hubs/download");

// Domain Event Subscriptions
var eventBus = app.Services.GetRequiredService<IEventBus>();

eventBus.Subscribe<DownloadCompletedEvent>((evt, ct) =>
{
    using var scope = app.Services.CreateScope();
    var jobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
    jobClient.Enqueue<UpdateQuotaJob>(j => j.ExecuteAsync(evt.UserId));
    return Task.CompletedTask;
});

eventBus.Subscribe<FileDeletedEvent>((evt, ct) =>
{
    using var scope = app.Services.CreateScope();
    var jobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
    jobClient.Enqueue<UpdateQuotaJob>(j => j.ExecuteAsync(evt.UserId));
    return Task.CompletedTask;
});

eventBus.Subscribe<UserWhitelistedEvent>((evt, ct) =>
{
    using var scope = app.Services.CreateScope();
    var jobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
    jobClient.Enqueue<SendWelcomeEmailJob>(j => j.ExecuteAsync(evt.UserId));
    return Task.CompletedTask;
});

eventBus.Subscribe<QuotaThresholdCrossedEvent>((evt, ct) =>
{
    using var scope = app.Services.CreateScope();
    var jobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
    jobClient.Enqueue<SendQuotaEmailJob>(j => j.ExecuteAsync(evt.UserId, evt.Threshold));
    return Task.CompletedTask;
});

app.Run();
