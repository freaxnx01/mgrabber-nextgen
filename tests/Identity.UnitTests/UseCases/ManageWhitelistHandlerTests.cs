using MusicGrabber.Modules.Identity.Application.Ports.Driven;
using MusicGrabber.Modules.Identity.Application.UseCases.ManageWhitelist;
using MusicGrabber.Modules.Identity.Domain;
using MusicGrabber.Shared;
using MusicGrabber.Shared.Events;

namespace MusicGrabber.Modules.Identity.UnitTests.UseCases;

public sealed class ManageWhitelistHandlerTests
{
    private readonly IWhitelistRepository _repo = Substitute.For<IWhitelistRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly ManageWhitelistHandler _handler;

    public ManageWhitelistHandlerTests()
    {
        _handler = new ManageWhitelistHandler(_repo, _eventBus);
    }

    [Fact]
    public async Task AddAsync_NewUser_AddsEntryAndPublishesEvent()
    {
        var userId = "newuser@example.com";

        var result = await _handler.AddAsync(userId, "User", "admin@example.com");

        result.UserId.Should().Be(userId);
        result.Role.Should().Be("User");
        result.IsActive.Should().BeTrue();
        await _repo.Received(1).AddAsync(Arg.Is<WhitelistEntry>(e => e.UserId == userId), Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Is<UserWhitelistedEvent>(e => e.UserId == userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleAsync_ActiveEntry_DeactivatesIt()
    {
        var entry = new WhitelistEntry { Id = Guid.NewGuid(), UserId = "user@example.com", IsActive = true };
        _repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<WhitelistEntry> { entry });

        var result = await _handler.ToggleAsync(entry.Id);

        result.IsActive.Should().BeFalse();
        await _repo.Received(1).UpdateAsync(Arg.Is<WhitelistEntry>(e => e.Id == entry.Id && !e.IsActive), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_ExistingEntry_CallsDelete()
    {
        var id = Guid.NewGuid();

        await _handler.RemoveAsync(id);

        await _repo.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntries()
    {
        var entries = new List<WhitelistEntry>
        {
            new() { UserId = "a@example.com" },
            new() { UserId = "b@example.com" }
        };
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _handler.GetAllAsync();

        result.Should().HaveCount(2);
    }
}
