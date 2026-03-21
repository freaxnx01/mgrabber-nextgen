using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Domain;

namespace MusicGrabber.Modules.Download.Application.UseCases.GetJobStatus;

public sealed class GetJobStatusHandler(IDownloadJobRepository repo)
{
    public Task<DownloadJob?> GetAsync(Guid jobId, CancellationToken ct = default)
        => repo.GetByIdAsync(jobId, ct);

    public Task<List<DownloadJob>> GetUserJobsAsync(string userId, CancellationToken ct = default)
        => repo.GetByUserIdAsync(userId, ct);
}
