using MusicGrabber.Modules.Download.Application.Ports.Driving;
using MusicGrabber.Shared.Contracts;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Download.Infrastructure;

internal sealed class DownloadFacadeAdapter(IDownloadService downloadService) : IDownloadFacade
{
    public Task<Guid> StartAsync(StartDownloadRequest request, CancellationToken ct = default)
        => downloadService.StartDownloadAsync(request, ct);
}
