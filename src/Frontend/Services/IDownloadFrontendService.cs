using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Services;

public interface IDownloadFrontendService
{
    Task<List<YouTubeSearchResultDto>> SearchYouTubeAsync(string query, int maxResults = 10, CancellationToken ct = default);
    Task<Guid> StartDownloadAsync(StartDownloadRequest request, CancellationToken ct = default);
    Task<DownloadJob?> GetJobStatusAsync(Guid jobId, CancellationToken ct = default);
    Task<List<DownloadJob>> GetUserJobsAsync(string userId, CancellationToken ct = default);
    Task<List<DownloadJob>> GetUserFilesAsync(string userId, CancellationToken ct = default);
    Task DeleteFileAsync(Guid jobId, string userId, CancellationToken ct = default);
    Task<UserStatsDto> GetUserStatsAsync(string userId, CancellationToken ct = default);
    Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken ct = default);
}
