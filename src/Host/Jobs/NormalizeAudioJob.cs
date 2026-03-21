using Hangfire;
using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Infrastructure.Adapters.Persistence;

namespace MusicGrabber.Host.Jobs;

public sealed class NormalizeAudioJob(
    DownloadDbContext db,
    IAudioNormalizer normalizer,
    ILogger<NormalizeAudioJob> logger)
{
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [2, 4])]
    public async Task ExecuteAsync(Guid jobId)
    {
        var job = await db.DownloadJobs.FindAsync(jobId);
        if (job?.FilePath is null) return;

        job.MarkNormalizing();
        await db.SaveChangesAsync();

        var outputPath = Path.Combine(
            Path.GetDirectoryName(job.FilePath)!,
            $"norm_{Path.GetFileName(job.FilePath)}");

        var result = await normalizer.NormalizeAsync(job.FilePath, outputPath);

        if (result.Success)
        {
            File.Delete(job.FilePath);
            File.Move(result.OutputPath, job.FilePath);
            job.MarkCompleted(job.FilePath, new FileInfo(job.FilePath).Length);
        }
        else
        {
            logger.LogWarning("Normalization failed for {JobId}: {Error}", jobId, result.Error);
        }

        await db.SaveChangesAsync();
    }
}
