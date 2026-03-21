using MusicGrabber.Modules.Identity.Application.Ports.Driven;
using MusicGrabber.Modules.Identity.Application.UseCases.GetUserProfile;
using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.UnitTests.UseCases;

public sealed class GetUserProfileHandlerTests
{
    private readonly IWhitelistRepository _whitelistRepository = Substitute.For<IWhitelistRepository>();
    private readonly GetUserProfileHandler _handler;

    public GetUserProfileHandlerTests()
    {
        _handler = new GetUserProfileHandler(_whitelistRepository);
    }

    [Fact]
    public async Task HandleAsync_WhitelistedUser_ReturnsProfile()
    {
        var userId = "user@example.com";
        var entry = new WhitelistEntry
        {
            UserId = userId,
            Role = "User",
            AddedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        _whitelistRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(entry);

        var result = await _handler.HandleAsync(userId);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Role.Should().Be("User");
        result.CreatedAt.Should().Be(entry.AddedAt);
    }

    [Fact]
    public async Task HandleAsync_UnknownUser_ReturnsNull()
    {
        var userId = "unknown@example.com";
        _whitelistRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((WhitelistEntry?)null);

        var result = await _handler.HandleAsync(userId);

        result.Should().BeNull();
    }
}
