using Frontend.Auth;
using Frontend.Components;
using Frontend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "DevAuth";
    options.DefaultAuthenticateScheme = "DevAuth";
    options.DefaultChallengeScheme = "DevAuth";
})
.AddDevAuth("DevAuth", options =>
{
    // Configure dev user from appsettings or defaults
    var devAuthConfig = builder.Configuration.GetSection("DevAuth");
    options.Email = devAuthConfig["Email"] ?? "dev@test.local";
    options.Name = devAuthConfig["Name"] ?? "Dev User";
    options.Role = devAuthConfig["Role"] ?? "User";
});

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

app.Run();
