using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MusicGrabber.Modules.Quota.Application.Ports.Driven;

namespace MusicGrabber.Modules.Quota.Infrastructure.Adapters;

internal sealed class SmtpEmailAdapter : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailAdapter> _logger;

    public SmtpEmailAdapter(IConfiguration config, ILogger<SmtpEmailAdapter> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task SendQuotaWarningAsync(string userId, long usedBytes, long quotaBytes, CancellationToken ct = default)
    {
        var subject = "MusicGrabber: Storage quota warning";
        var usedGb = usedBytes / 1_073_741_824.0;
        var quotaGb = quotaBytes / 1_073_741_824.0;
        var body = $"Your storage usage has reached the warning level.\n\n" +
                   $"Used: {usedGb:F2} GB / {quotaGb:F2} GB\n\n" +
                   "Consider deleting files to free up space.";

        return SendAsync(userId, subject, body, ct);
    }

    public Task SendQuotaCriticalAsync(string userId, long usedBytes, long quotaBytes, CancellationToken ct = default)
    {
        var subject = "MusicGrabber: Storage quota critical";
        var usedGb = usedBytes / 1_073_741_824.0;
        var quotaGb = quotaBytes / 1_073_741_824.0;
        var body = $"Your storage usage is critically high.\n\n" +
                   $"Used: {usedGb:F2} GB / {quotaGb:F2} GB\n\n" +
                   "Downloads will be blocked when the quota is full. Please delete files immediately.";

        return SendAsync(userId, subject, body, ct);
    }

    public Task SendQuotaBlockedAsync(string userId, long usedBytes, long quotaBytes, CancellationToken ct = default)
    {
        var subject = "MusicGrabber: Storage quota exceeded — downloads blocked";
        var usedGb = usedBytes / 1_073_741_824.0;
        var quotaGb = quotaBytes / 1_073_741_824.0;
        var body = $"Your storage quota has been exceeded.\n\n" +
                   $"Used: {usedGb:F2} GB / {quotaGb:F2} GB\n\n" +
                   "New downloads are blocked until you free up space.";

        return SendAsync(userId, subject, body, ct);
    }

    private async Task SendAsync(string toAddress, string subject, string body, CancellationToken ct)
    {
        var host = _config["SMTP_HOST"] ?? throw new InvalidOperationException("SMTP_HOST is not configured.");
        var portStr = _config["SMTP_PORT"] ?? "587";
        var password = _config["SMTP_PASSWORD"] ?? throw new InvalidOperationException("SMTP_PASSWORD is not configured.");
        var from = _config["SMTP_FROM"] ?? throw new InvalidOperationException("SMTP_FROM is not configured.");

        if (!int.TryParse(portStr, out var port))
            port = 587;

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(from, password),
            EnableSsl = true
        };

        using var message = new MailMessage(from, toAddress, subject, body);

        try
        {
            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Quota notification email sent to {UserId}: {Subject}", toAddress, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send quota notification email to {UserId}: {Subject}", toAddress, subject);
            throw;
        }
    }
}
