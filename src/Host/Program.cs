using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MusicGrabber.Modules.Identity.Domain;
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

// Hangfire — Hangfire.Storage.SQLite uses sqlite-net-pcl which expects a file path, not a connection string
var hangfireDbPath = connectionString.Replace("Data Source=", "").Replace("data source=", "").Trim();
builder.Services.AddHangfire(config =>
    config.UseSQLiteStorage(hangfireDbPath));
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

// Google OAuth + optional Authentik OIDC
// Note: AddIdentity (in Identity module) already sets default scheme to cookies.
// We only add external providers here — don't call AddAuthentication() again as it
// would override the cookie defaults and cause infinite auth loops.
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? "";
        options.ClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? "";
    });

// Only register Authentik if client ID is configured
var authentikClientId = builder.Configuration["AUTHENTIK_CLIENT_ID"];
if (!string.IsNullOrEmpty(authentikClientId))
{
    builder.Services.AddAuthentication().AddOpenIdConnect("Authentik", "Authentik", options =>
    {
        options.Authority = builder.Configuration["AUTHENTIK_AUTHORITY"] ?? "https://auth.home.freaxnx01.ch/application/o/musicgrabber/";
        options.ClientId = authentikClientId;
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
}

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
// Forwarded headers — required behind reverse proxy (Traefik) so OAuth
// redirect URIs use https:// instead of http://
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
// Trust all proxies — Traefik connects from Docker network, not loopback
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);
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
app.MapStaticAssets();
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
app.MapGet("/api/auth/google-login", (string? returnUrl) =>
{
    var callbackUrl = $"/api/auth/external-callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = callbackUrl },
        ["Google"]);
}).AllowAnonymous();

app.MapGet("/api/auth/authentik-login", (string? returnUrl) =>
{
    var callbackUrl = $"/api/auth/external-callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = callbackUrl },
        ["Authentik"]);
}).AllowAnonymous();

app.MapGet("/api/auth/external-callback", async (
    string? returnUrl,
    HttpContext httpContext,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<Program> logger) =>
{
    // Authenticate against the external cookie scheme directly
    var authResult = await httpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
    if (!authResult.Succeeded || authResult.Principal is null)
    {
        logger.LogWarning("External cookie authentication failed");
        return Results.Redirect("/login");
    }

    var externalPrincipal = authResult.Principal;
    var props = authResult.Properties?.Items;
    var provider = (props is not null && props.TryGetValue("LoginProvider", out var lp) ? lp : null)
        ?? externalPrincipal.Identity?.AuthenticationType ?? "Unknown";
    var providerKey = externalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    var email = externalPrincipal.FindFirstValue(ClaimTypes.Email);

    logger.LogInformation("External login from {Provider}, key={Key}, email={Email}", provider, providerKey, email);

    if (string.IsNullOrWhiteSpace(email))
    {
        logger.LogWarning("No email claim found in external principal");
        return Results.Redirect("/login");
    }

    // Try to sign in with existing external login
    var result = await signInManager.ExternalLoginSignInAsync(provider, providerKey, isPersistent: true);

    if (!result.Succeeded)
    {
        logger.LogInformation("ExternalLoginSignIn failed, creating user for {Email}", email);

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { Id = email, UserName = email, Email = email };
            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return Results.Redirect("/login");
            }
        }

        var loginInfo = new UserLoginInfo(provider, providerKey, provider);
        var addLoginResult = await userManager.AddLoginAsync(user, loginInfo);
        if (!addLoginResult.Succeeded)
        {
            logger.LogError("Failed to add login: {Errors}",
                string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
        }

        await signInManager.SignInAsync(user, isPersistent: true);
    }

    // Clean up external cookie
    await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);

    logger.LogInformation("User {Email} signed in successfully", email);
    return Results.Redirect(returnUrl ?? "/");
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
