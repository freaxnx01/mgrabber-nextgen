using MusicGrabber.Modules.Identity.Application.Ports.Driven;
using MusicGrabber.Modules.Identity.Application.UseCases.UpdateSettings;
using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.UnitTests.UseCases;

public sealed class UpdateSettingsHandlerTests
{
    private readonly IUserSettingsRepository _repo = Substitute.For<IUserSettingsRepository>();
    private readonly UpdateSettingsHandler _handler;

    public UpdateSettingsHandlerTests()
    {
        _handler = new UpdateSettingsHandler(_repo);
    }

    [Fact]
    public async Task GetOrCreateAsync_NoExistingSettings_CreatesDefaultSettings()
    {
        var userId = "user@example.com";
        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserSettings?)null);

        var result = await _handler.GetOrCreateAsync(userId);

        result.UserId.Should().Be(userId);
        result.DefaultFormat.Should().Be("Mp3");
        result.EnableNormalization.Should().BeTrue();
        result.NormalizationLufs.Should().Be(-14);
        await _repo.Received(1).UpsertAsync(Arg.Any<UserSettings>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrCreateAsync_ExistingSettings_ReturnsExisting()
    {
        var userId = "user@example.com";
        var existing = new UserSettings { UserId = userId, DefaultFormat = "Flac" };
        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.GetOrCreateAsync(userId);

        result.DefaultFormat.Should().Be("Flac");
        await _repo.DidNotReceive().UpsertAsync(Arg.Any<UserSettings>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ValidSettings_PersistsAndReturnsUpdated()
    {
        var userId = "user@example.com";
        var existing = new UserSettings { UserId = userId, DefaultFormat = "Mp3" };
        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.UpdateAsync(userId, "Flac", false, -16, false);

        result.DefaultFormat.Should().Be("Flac");
        result.EnableNormalization.Should().BeFalse();
        result.NormalizationLufs.Should().Be(-16);
        result.EmailNotifications.Should().BeFalse();
        await _repo.Received(1).UpsertAsync(Arg.Any<UserSettings>(), Arg.Any<CancellationToken>());
    }
}
