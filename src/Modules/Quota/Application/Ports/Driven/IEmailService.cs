namespace MusicGrabber.Modules.Quota.Application.Ports.Driven;

/// <summary>Quota-related email notifications.</summary>
public interface IEmailService
{
    Task SendQuotaWarningAsync(string userId, long usedBytes, long quotaBytes, CancellationToken ct = default);
    Task SendQuotaCriticalAsync(string userId, long usedBytes, long quotaBytes, CancellationToken ct = default);
    Task SendQuotaBlockedAsync(string userId, long usedBytes, long quotaBytes, CancellationToken ct = default);
}
