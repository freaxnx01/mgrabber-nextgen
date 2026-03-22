using Hangfire;
using Microsoft.AspNetCore.SignalR;
using MusicGrabber.Host.Hubs;
using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Quota.Application.Ports.Driving;

namespace MusicGrabber.Host.Jobs;

public sealed class UpdateQuotaJob(
    IQuotaService quotaService,
    IDownloadJobRepository downloadRepo,
    IHubContext<DownloadHub> hubContext)
{
    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(string userId)
    {
        var totalBytes = await downloadRepo.GetTotalFileSizeByUserIdAsync(userId);
        var fileCount = await downloadRepo.GetFileCountByUserIdAsync(userId);
        await quotaService.RecalculateUsageAsync(userId, totalBytes, fileCount);

        // Errata #22: Send quota update via SignalR
        var quotaInfo = await quotaService.GetQuotaAsync(userId);
        await hubContext.Clients.Group(userId).SendAsync("ReceiveQuotaUpdate", quotaInfo);
    }
}
