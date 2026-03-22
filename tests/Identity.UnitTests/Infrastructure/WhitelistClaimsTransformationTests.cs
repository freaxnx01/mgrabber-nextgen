using System.Security.Claims;
using MusicGrabber.Modules.Identity.Application.Ports.Driven;
using MusicGrabber.Modules.Identity.Domain;
using MusicGrabber.Modules.Identity.Infrastructure.Adapters;

namespace MusicGrabber.Modules.Identity.UnitTests.Infrastructure;

public sealed class WhitelistClaimsTransformationTests
{
    private readonly IWhitelistRepository _repo = Substitute.For<IWhitelistRepository>();
    private readonly WhitelistClaimsTransformation _transformation;

    public WhitelistClaimsTransformationTests()
    {
        _transformation = new WhitelistClaimsTransformation(_repo);
    }

    [Fact]
    public async Task TransformAsync_WhitelistedActiveUser_AddsRoleClaim()
    {
        var email = "user@example.com";
        var principal = CreatePrincipal(email);
        _repo.GetByUserIdAsync(email, Arg.Any<CancellationToken>())
            .Returns(new WhitelistEntry { UserId = email, Role = "User", IsActive = true });

        var result = await _transformation.TransformAsync(principal);

        result.HasClaim(ClaimTypes.Role, "User").Should().BeTrue();
    }

    [Fact]
    public async Task TransformAsync_InactiveWhitelistEntry_DoesNotAddRoleClaim()
    {
        var email = "inactive@example.com";
        var principal = CreatePrincipal(email);
        _repo.GetByUserIdAsync(email, Arg.Any<CancellationToken>())
            .Returns(new WhitelistEntry { UserId = email, Role = "User", IsActive = false });

        var result = await _transformation.TransformAsync(principal);

        result.HasClaim(ClaimTypes.Role, "User").Should().BeFalse();
    }

    [Fact]
    public async Task TransformAsync_UserNotInWhitelist_DoesNotAddRoleClaim()
    {
        var email = "unknown@example.com";
        var principal = CreatePrincipal(email);
        _repo.GetByUserIdAsync(email, Arg.Any<CancellationToken>())
            .Returns((WhitelistEntry?)null);

        var result = await _transformation.TransformAsync(principal);

        result.HasClaim(c => c.Type == ClaimTypes.Role).Should().BeFalse();
    }

    [Fact]
    public async Task TransformAsync_AdminUser_AddsAdminRoleClaim()
    {
        var email = "admin@example.com";
        var principal = CreatePrincipal(email);
        _repo.GetByUserIdAsync(email, Arg.Any<CancellationToken>())
            .Returns(new WhitelistEntry { UserId = email, Role = "Admin", IsActive = true });

        var result = await _transformation.TransformAsync(principal);

        result.HasClaim(ClaimTypes.Role, "Admin").Should().BeTrue();
    }

    private static ClaimsPrincipal CreatePrincipal(string email)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, email) },
            "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
