using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Download.Application.UseCases.GetDownloadStats;

public sealed class GetDownloadStatsHandler(IDownloadJobRepository repo)
{
    public Task<UserStatsDto> GetUserStatsAsync(string userId, CancellationToken ct = default)
        => repo.GetUserStatsAsync(userId, ct);

    public Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken ct = default)
        => repo.GetGlobalStatsAsync(ct);
}
