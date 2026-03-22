using MusicGrabber.Modules.Download.Application.Ports.Driving;
using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Services;

public sealed class DownloadFrontendService(IDownloadService downloadService) : IDownloadFrontendService
{
    public Task<List<YouTubeSearchResultDto>> SearchYouTubeAsync(string query, int maxResults, CancellationToken ct)
        => downloadService.SearchYouTubeAsync(query, maxResults, ct);

    public Task<Guid> StartDownloadAsync(StartDownloadRequest request, CancellationToken ct)
        => downloadService.StartDownloadAsync(request, ct);

    public Task<DownloadJob?> GetJobStatusAsync(Guid jobId, CancellationToken ct)
        => downloadService.GetJobStatusAsync(jobId, ct);

    public Task<List<DownloadJob>> GetUserJobsAsync(string userId, CancellationToken ct)
        => downloadService.GetUserJobsAsync(userId, ct);

    public Task<List<DownloadJob>> GetUserFilesAsync(string userId, CancellationToken ct)
        => downloadService.GetUserFilesAsync(userId, ct);

    public Task DeleteFileAsync(Guid jobId, string userId, CancellationToken ct)
        => downloadService.DeleteFileAsync(jobId, userId, ct);

    public Task<UserStatsDto> GetUserStatsAsync(string userId, CancellationToken ct)
        => downloadService.GetUserStatsAsync(userId, ct);

    public Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken ct)
        => downloadService.GetGlobalStatsAsync(ct);
}
