using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Download.Application.Ports.Driven;

public interface IDownloadJobRepository
{
    Task<DownloadJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<DownloadJob>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<List<DownloadJob>> GetCompletedByUserIdAsync(string userId, CancellationToken ct = default);
    Task<int> GetActiveCountByUserIdAsync(string userId, CancellationToken ct = default);
    Task<int> GetActiveCountAsync(CancellationToken ct = default);
    Task AddAsync(DownloadJob job, CancellationToken ct = default);
    Task UpdateAsync(DownloadJob job, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<UserStatsDto> GetUserStatsAsync(string userId, CancellationToken ct = default);
    Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken ct = default);
    Task<long> GetTotalFileSizeByUserIdAsync(string userId, CancellationToken ct = default);
    Task<int> GetFileCountByUserIdAsync(string userId, CancellationToken ct = default);
}
