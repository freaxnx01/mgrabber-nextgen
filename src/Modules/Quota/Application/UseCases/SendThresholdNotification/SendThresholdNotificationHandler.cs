using MusicGrabber.Modules.Quota.Application.Ports.Driven;
using MusicGrabber.Modules.Quota.Domain;

namespace MusicGrabber.Modules.Quota.Application.UseCases.SendThresholdNotification;

public sealed class SendThresholdNotificationHandler
{
    private static readonly TimeSpan EmailCooldown = TimeSpan.FromHours(24);

    private readonly IQuotaRepository _repo;
    private readonly IEmailService _email;

    public SendThresholdNotificationHandler(IQuotaRepository repo, IEmailService email)
    {
        _repo = repo;
        _email = email;
    }

    public async Task HandleAsync(string userId, string threshold, CancellationToken ct = default)
    {
        var quota = await _repo.GetByUserIdAsync(userId, ct)
            ?? throw new InvalidOperationException($"Quota record not found for user '{userId}'.");

        // Normal threshold requires no email notification
        if (threshold == QuotaThreshold.Normal)
            return;

        // Rate limit: skip if same threshold email was sent within the last 24 hours
        if (quota.LastEmailSentAt.HasValue &&
            quota.LastEmailThreshold == threshold &&
            DateTime.UtcNow - quota.LastEmailSentAt.Value < EmailCooldown)
        {
            return;
        }

        await SendEmailAsync(userId, threshold, quota.UsedBytes, quota.QuotaBytes, ct);

        quota.LastEmailSentAt = DateTime.UtcNow;
        quota.LastEmailThreshold = threshold;
        await _repo.UpdateAsync(quota, ct);
    }

    private Task SendEmailAsync(
        string userId,
        string threshold,
        long usedBytes,
        long quotaBytes,
        CancellationToken ct)
    {
        return threshold switch
        {
            QuotaThreshold.Warning => _email.SendQuotaWarningAsync(userId, usedBytes, quotaBytes, ct),
            QuotaThreshold.Critical => _email.SendQuotaCriticalAsync(userId, usedBytes, quotaBytes, ct),
            QuotaThreshold.Blocked => _email.SendQuotaBlockedAsync(userId, usedBytes, quotaBytes, ct),
            _ => Task.CompletedTask
        };
    }
}
