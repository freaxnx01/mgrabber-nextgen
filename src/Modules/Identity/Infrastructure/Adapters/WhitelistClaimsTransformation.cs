using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using MusicGrabber.Modules.Identity.Application.Ports.Driven;

namespace MusicGrabber.Modules.Identity.Infrastructure.Adapters;

public sealed class WhitelistClaimsTransformation : IClaimsTransformation
{
    private readonly IWhitelistRepository _repo;

    public WhitelistClaimsTransformation(IWhitelistRepository repo)
    {
        _repo = repo;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return principal;
        }

        var entry = await _repo.GetByUserIdAsync(email);
        if (entry is null || !entry.IsActive)
        {
            return principal;
        }

        var claimsIdentity = new ClaimsIdentity();
        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, entry.Role));

        principal.AddIdentity(claimsIdentity);

        return principal;
    }
}
