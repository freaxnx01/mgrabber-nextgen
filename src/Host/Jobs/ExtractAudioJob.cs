using Hangfire;
using Microsoft.AspNetCore.SignalR;
using MusicGrabber.Host.Hubs;
using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Infrastructure.Adapters.Persistence;
using MusicGrabber.Shared;
using MusicGrabber.Shared.Events;

namespace MusicGrabber.Host.Jobs;

public sealed class ExtractAudioJob(
    DownloadDbContext db,
    IAudioExtractor extractor,
    IHubContext<DownloadHub> hubContext,
    IEventBus eventBus,
    ILogger<ExtractAudioJob> logger)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [2, 4, 8])]
    public async Task ExecuteAsync(Guid jobId)
    {
        var job = await db.DownloadJobs.FindAsync(jobId);
        if (job is null) return;

        try
        {
            job.MarkDownloading();
            await db.SaveChangesAsync();
            await hubContext.Clients.Group(job.UserId).SendAsync("ReceiveProgress", jobId, 0, "Downloading");

            var storageDir = Path.Combine("/storage", job.UserId);
            Directory.CreateDirectory(storageDir);

            var result = await extractor.ExtractAsync(
                job.Url, job.Format.ToString(), storageDir,
                job.UserId, job.Id.ToString(), job.Author, job.Title);

            job.UpdateFilenames(result.OriginalFilename, result.CorrectedFilename);
            job.MarkCompleted(result.FilePath, result.FileSizeBytes);
            await db.SaveChangesAsync();

            await hubContext.Clients.Group(job.UserId).SendAsync("ReceiveCompleted", jobId,
                new { job.Title, job.Author, job.Format, job.FileSizeBytes });

            // Errata #21: Only publish event; quota/email jobs are triggered by event subscriptions
            await eventBus.PublishAsync(new DownloadCompletedEvent(jobId, job.UserId, result.FileSizeBytes));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Extraction failed for job {JobId}", jobId);
            job.IncrementRetry();

            if (!job.CanRetry)
            {
                job.MarkFailed(ex.Message);
                await db.SaveChangesAsync();
                await hubContext.Clients.Group(job.UserId).SendAsync("ReceiveFailed", jobId, ex.Message);
            }
            else
            {
                await db.SaveChangesAsync();
            }

            throw; // Let Hangfire handle retry
        }
    }
}
