using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Domain;

namespace MusicGrabber.Modules.Download.Application.UseCases.ListUserFiles;

public sealed class ListUserFilesHandler(IDownloadJobRepository repo)
{
    public Task<List<DownloadJob>> GetFilesAsync(string userId, CancellationToken ct = default)
        => repo.GetCompletedByUserIdAsync(userId, ct);
}
