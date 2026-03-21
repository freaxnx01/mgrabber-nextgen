using MusicGrabber.Modules.Quota.Application.Ports.Driven;
using MusicGrabber.Modules.Quota.Application.UseCases.CheckQuota;
using MusicGrabber.Modules.Quota.Domain;
using MusicGrabber.Shared;
using MusicGrabber.Shared.Events;

namespace MusicGrabber.Modules.Quota.UnitTests.UseCases;

public sealed class CheckQuotaHandlerTests
{
    private readonly IQuotaRepository _repo = Substitute.For<IQuotaRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly CheckQuotaHandler _handler;

    public CheckQuotaHandlerTests()
    {
        _handler = new CheckQuotaHandler(_repo, _eventBus);
    }

    // ─── GetQuotaAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuotaAsync_ExistingUser_ReturnsQuotaDto()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 500_000_000L,
            FileCount = 10,
            CurrentThreshold = QuotaThreshold.Normal
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        var result = await _handler.GetQuotaAsync("user@example.com");

        result.UserId.Should().Be("user@example.com");
        result.QuotaBytes.Should().Be(1_073_741_824L);
        result.UsedBytes.Should().Be(500_000_000L);
        result.FileCount.Should().Be(10);
        result.CurrentThreshold.Should().Be(QuotaThreshold.Normal);
    }

    [Fact]
    public async Task GetQuotaAsync_NonExistentUser_ThrowsInvalidOperationException()
    {
        _repo.GetByUserIdAsync("unknown@example.com", Arg.Any<CancellationToken>()).Returns((UserQuota?)null);

        var act = () => _handler.GetQuotaAsync("unknown@example.com");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── CheckAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_SufficientSpace_ReturnsTrue()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 100_000_000L,
            CurrentThreshold = QuotaThreshold.Normal
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        var result = await _handler.CheckAsync("user@example.com", 50_000_000L);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_InsufficientSpace_ReturnsFalse()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 1_050_000_000L,
            CurrentThreshold = QuotaThreshold.Warning
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        var result = await _handler.CheckAsync("user@example.com", 50_000_000L);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_UserBlocked_ReturnsFalse()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 1_073_741_824L,
            CurrentThreshold = QuotaThreshold.Blocked
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        var result = await _handler.CheckAsync("user@example.com", 1L);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_NonExistentUser_ReturnsFalse()
    {
        _repo.GetByUserIdAsync("unknown@example.com", Arg.Any<CancellationToken>()).Returns((UserQuota?)null);

        var result = await _handler.CheckAsync("unknown@example.com", 1L);

        result.Should().BeFalse();
    }

    // ─── RecalculateUsageAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RecalculateUsageAsync_ThresholdChanges_PublishesEvent()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 100L,
            UsedBytes = 0L,
            CurrentThreshold = QuotaThreshold.Normal
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        // Simulate that recalculation moves to Warning (70 bytes out of 100)
        await _handler.RecalculateUsageAsync("user@example.com", 70L, 5);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<QuotaThresholdCrossedEvent>(e =>
                e.UserId == "user@example.com" && e.Threshold == QuotaThreshold.Warning),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecalculateUsageAsync_SameThreshold_DoesNotPublishEvent()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 100L,
            UsedBytes = 70L,
            CurrentThreshold = QuotaThreshold.Warning
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        // Recalculate, still Warning
        await _handler.RecalculateUsageAsync("user@example.com", 72L, 5);

        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<QuotaThresholdCrossedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecalculateUsageAsync_UpdatesStoredValues()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 100L,
            UsedBytes = 10L,
            FileCount = 2,
            CurrentThreshold = QuotaThreshold.Normal
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        await _handler.RecalculateUsageAsync("user@example.com", 50L, 8);

        await _repo.Received(1).UpdateAsync(
            Arg.Is<UserQuota>(q =>
                q.UsedBytes == 50L &&
                q.FileCount == 8 &&
                q.CurrentThreshold == QuotaThreshold.Normal),
            Arg.Any<CancellationToken>());
    }

    // ─── InitializeUserAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task InitializeUserAsync_NewUser_CreatesQuotaRecord()
    {
        _repo.GetByUserIdAsync("newuser@example.com", Arg.Any<CancellationToken>()).Returns((UserQuota?)null);

        await _handler.InitializeUserAsync("newuser@example.com");

        await _repo.Received(1).AddAsync(
            Arg.Is<UserQuota>(q =>
                q.UserId == "newuser@example.com" &&
                q.QuotaBytes == UserQuota.DefaultQuotaBytes &&
                q.UsedBytes == 0L &&
                q.CurrentThreshold == QuotaThreshold.Normal),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeUserAsync_ExistingUser_DoesNotCreateDuplicate()
    {
        var existing = new UserQuota { UserId = "existing@example.com" };
        _repo.GetByUserIdAsync("existing@example.com", Arg.Any<CancellationToken>()).Returns(existing);

        await _handler.InitializeUserAsync("existing@example.com");

        await _repo.DidNotReceive().AddAsync(Arg.Any<UserQuota>(), Arg.Any<CancellationToken>());
    }
}
