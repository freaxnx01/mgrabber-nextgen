using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Shared;
using MusicGrabber.Shared.Events;

namespace MusicGrabber.Modules.Download.Application.UseCases.DeleteFile;

public sealed class DeleteFileHandler(
    IDownloadJobRepository repo,
    IFileStorage fileStorage,
    IEventBus eventBus)
{
    public async Task DeleteAsync(Guid jobId, string userId, CancellationToken ct = default)
    {
        var job = await repo.GetByIdAsync(jobId, ct)
            ?? throw new InvalidOperationException($"Job {jobId} not found.");

        if (job.UserId != userId)
            throw new UnauthorizedAccessException("Cannot delete another user's file.");

        if (!string.IsNullOrEmpty(job.FilePath) && await fileStorage.ExistsAsync(job.FilePath))
            await fileStorage.DeleteAsync(job.FilePath);

        var fileSize = job.FileSizeBytes;
        await repo.DeleteAsync(jobId, ct);
        await eventBus.PublishAsync(new FileDeletedEvent(jobId, userId, fileSize), ct);
    }
}
