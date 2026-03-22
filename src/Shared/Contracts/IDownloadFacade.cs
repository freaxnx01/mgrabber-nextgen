using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Shared.Contracts;

public interface IDownloadFacade
{
    Task<Guid> StartAsync(StartDownloadRequest request, CancellationToken ct = default);
}
