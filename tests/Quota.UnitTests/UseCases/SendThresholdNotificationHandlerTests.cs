using MusicGrabber.Modules.Quota.Application.Ports.Driven;
using MusicGrabber.Modules.Quota.Application.UseCases.SendThresholdNotification;
using MusicGrabber.Modules.Quota.Domain;

namespace MusicGrabber.Modules.Quota.UnitTests.UseCases;

public sealed class SendThresholdNotificationHandlerTests
{
    private readonly IQuotaRepository _repo = Substitute.For<IQuotaRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly SendThresholdNotificationHandler _handler;

    public SendThresholdNotificationHandlerTests()
    {
        _handler = new SendThresholdNotificationHandler(_repo, _email);
    }

    [Fact]
    public async Task HandleAsync_WarningThreshold_SendsWarningEmail()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 800_000_000L,
            CurrentThreshold = QuotaThreshold.Warning,
            LastEmailSentAt = null,
            LastEmailThreshold = null
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        await _handler.HandleAsync("user@example.com", QuotaThreshold.Warning);

        await _email.Received(1).SendQuotaWarningAsync(
            "user@example.com", 800_000_000L, 1_073_741_824L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CriticalThreshold_SendsCriticalEmail()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 950_000_000L,
            CurrentThreshold = QuotaThreshold.Critical,
            LastEmailSentAt = null,
            LastEmailThreshold = null
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        await _handler.HandleAsync("user@example.com", QuotaThreshold.Critical);

        await _email.Received(1).SendQuotaCriticalAsync(
            "user@example.com", 950_000_000L, 1_073_741_824L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BlockedThreshold_SendsBlockedEmail()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 1_073_741_824L,
            CurrentThreshold = QuotaThreshold.Blocked,
            LastEmailSentAt = null,
            LastEmailThreshold = null
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        await _handler.HandleAsync("user@example.com", QuotaThreshold.Blocked);

        await _email.Received(1).SendQuotaBlockedAsync(
            "user@example.com", 1_073_741_824L, 1_073_741_824L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecentlySentSameThreshold_SkipsEmail()
    {
        // Email was sent less than 24 hours ago for the same threshold
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 800_000_000L,
            CurrentThreshold = QuotaThreshold.Warning,
            LastEmailSentAt = DateTime.UtcNow.AddHours(-12),
            LastEmailThreshold = QuotaThreshold.Warning
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        await _handler.HandleAsync("user@example.com", QuotaThreshold.Warning);

        await _email.DidNotReceive().SendQuotaWarningAsync(
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailSentOver24HoursAgo_SendsEmail()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 800_000_000L,
            CurrentThreshold = QuotaThreshold.Warning,
            LastEmailSentAt = DateTime.UtcNow.AddHours(-25),
            LastEmailThreshold = QuotaThreshold.Warning
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        await _handler.HandleAsync("user@example.com", QuotaThreshold.Warning);

        await _email.Received(1).SendQuotaWarningAsync(
            "user@example.com", 800_000_000L, 1_073_741_824L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DifferentThresholdThanLastEmail_SendsEmail()
    {
        // Last email was for Warning, now it's Critical — should send even if < 24h
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 950_000_000L,
            CurrentThreshold = QuotaThreshold.Critical,
            LastEmailSentAt = DateTime.UtcNow.AddHours(-1),
            LastEmailThreshold = QuotaThreshold.Warning
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        await _handler.HandleAsync("user@example.com", QuotaThreshold.Critical);

        await _email.Received(1).SendQuotaCriticalAsync(
            "user@example.com", 950_000_000L, 1_073_741_824L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailSent_UpdatesLastEmailTracking()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 800_000_000L,
            CurrentThreshold = QuotaThreshold.Warning,
            LastEmailSentAt = null,
            LastEmailThreshold = null
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        await _handler.HandleAsync("user@example.com", QuotaThreshold.Warning);

        await _repo.Received(1).UpdateAsync(
            Arg.Is<UserQuota>(q =>
                q.LastEmailThreshold == QuotaThreshold.Warning &&
                q.LastEmailSentAt != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NormalThreshold_DoesNotSendEmail()
    {
        var quota = new UserQuota
        {
            UserId = "user@example.com",
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 100_000_000L,
            CurrentThreshold = QuotaThreshold.Normal,
            LastEmailSentAt = null,
            LastEmailThreshold = null
        };
        _repo.GetByUserIdAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(quota);

        await _handler.HandleAsync("user@example.com", QuotaThreshold.Normal);

        await _email.DidNotReceive().SendQuotaWarningAsync(
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendQuotaCriticalAsync(
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendQuotaBlockedAsync(
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        _repo.GetByUserIdAsync("unknown@example.com", Arg.Any<CancellationToken>()).Returns((UserQuota?)null);

        var act = () => _handler.HandleAsync("unknown@example.com", QuotaThreshold.Warning);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
