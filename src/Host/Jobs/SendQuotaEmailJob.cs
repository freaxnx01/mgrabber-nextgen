using Hangfire;
using MusicGrabber.Modules.Quota.Application.UseCases.SendThresholdNotification;

namespace MusicGrabber.Host.Jobs;

public sealed class SendQuotaEmailJob(SendThresholdNotificationHandler handler)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [2, 4, 8])]
    public async Task ExecuteAsync(string userId, string threshold)
    {
        await handler.HandleAsync(userId, threshold);
    }
}
