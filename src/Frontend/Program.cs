using Frontend.Auth;
using Frontend.Components;
using Frontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add authentication
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultAuthenticateScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
});

// Add cookie authentication
authBuilder.AddCookie("Cookies", options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
});

// Add Google OAuth if configured
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle("Google", options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.Scope.Add("email");
        options.Scope.Add("profile");
    });
    
    // Use Google as default challenge scheme when configured
    builder.Services.PostConfigure<AuthenticationOptions>(options =>
    {
        options.DefaultChallengeScheme = "Google";
    });
}
else
{
    // Fall back to DevAuth for development
    authBuilder.AddDevAuth("DevAuth", options =>
    {
        var devAuthConfig = builder.Configuration.GetSection("DevAuth");
        options.Email = devAuthConfig["Email"] ?? "admin@test.local";
        options.Name = devAuthConfig["Name"] ?? "Admin User";
        options.Role = devAuthConfig["Role"] ?? "Admin";
    });
}

builder.Services.AddAuthorization();

// Add HTTP client for Download API
builder.Services.AddHttpClient<DownloadApiService>(client =>
{
    var baseUrl = builder.Configuration["DownloadApi:BaseUrl"] ?? "http://download-api:8080";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["DownloadApi:ApiKey"] ?? "default-key");
});

builder.Services.AddScoped<DownloadApiService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Authentication endpoints
app.MapGet("/challenge/google", async (HttpContext context) =>
{
    await context.ChallengeAsync("Google", new AuthenticationProperties
    {
        RedirectUri = "/"
    });
});

app.MapGet("/signin-google", async (HttpContext context) =>
{
    var result = await context.AuthenticateAsync("Google");
    if (result?.Principal != null)
    {
        var claims = result.Principal.Claims.ToList();
        
        // Create claims identity with cookie authentication
        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);
        
        await context.SignInAsync("Cookies", principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        });
        
        context.Response.Redirect("/");
    }
    else
    {
        context.Response.Redirect("/login?ErrorMessage=Authentication failed");
    }
});

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync("Cookies");
    context.Response.Redirect("/login");
});

app.Run();
