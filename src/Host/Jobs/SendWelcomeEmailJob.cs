using Hangfire;
using MusicGrabber.Modules.Quota.Application.Ports.Driving;

namespace MusicGrabber.Host.Jobs;

public sealed class SendWelcomeEmailJob(
    IQuotaService quotaService,
    ILogger<SendWelcomeEmailJob> logger)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [2, 4, 8])]
    public async Task ExecuteAsync(string userId)
    {
        // Initialize the user's quota record as part of the welcome flow
        await quotaService.InitializeUserAsync(userId);
        logger.LogInformation("Welcome flow completed for user {UserId}", userId);
    }
}
