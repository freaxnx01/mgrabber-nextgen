using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Frontend.Auth;

public class DevAuthOptions : AuthenticationSchemeOptions
{
    public string Email { get; set; } = "dev@test.local";
    public string Name { get; set; } = "Dev User";
    public string Role { get; set; } = "User";
}

public class DevAuthHandler : AuthenticationHandler<DevAuthOptions>
{
    public DevAuthHandler(
        IOptionsMonitor<DevAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "dev-user-123"),
            new Claim(ClaimTypes.Email, Options.Email),
            new Claim(ClaimTypes.Name, Options.Name),
            new Claim(ClaimTypes.Role, Options.Role)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public static class DevAuthExtensions
{
    public static AuthenticationBuilder AddDevAuth(this AuthenticationBuilder builder, string authenticationScheme, Action<DevAuthOptions> configureOptions)
    {
        return builder.AddScheme<DevAuthOptions, DevAuthHandler>(authenticationScheme, configureOptions);
    }
}
