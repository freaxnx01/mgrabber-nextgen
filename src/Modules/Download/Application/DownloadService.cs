using MusicGrabber.Modules.Download.Application.Ports.Driving;
using MusicGrabber.Modules.Download.Application.UseCases.DeleteFile;
using MusicGrabber.Modules.Download.Application.UseCases.GetDownloadStats;
using MusicGrabber.Modules.Download.Application.UseCases.GetJobStatus;
using MusicGrabber.Modules.Download.Application.UseCases.ListUserFiles;
using MusicGrabber.Modules.Download.Application.UseCases.SearchYouTube;
using MusicGrabber.Modules.Download.Application.UseCases.StartDownload;
using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Download.Application;

public sealed class DownloadService(
    SearchYouTubeHandler searchHandler,
    StartDownloadHandler startDownloadHandler,
    GetJobStatusHandler jobStatusHandler,
    ListUserFilesHandler listUserFilesHandler,
    DeleteFileHandler deleteFileHandler,
    GetDownloadStatsHandler downloadStatsHandler) : IDownloadService
{
    public Task<List<YouTubeSearchResultDto>> SearchYouTubeAsync(string query, int maxResults = 10, CancellationToken ct = default)
        => searchHandler.SearchAsync(query, maxResults, ct);

    public Task<Guid> StartDownloadAsync(StartDownloadRequest request, CancellationToken ct = default)
        => startDownloadHandler.StartAsync(request, ct);

    public Task<DownloadJob?> GetJobStatusAsync(Guid jobId, CancellationToken ct = default)
        => jobStatusHandler.GetAsync(jobId, ct);

    public Task<List<DownloadJob>> GetUserJobsAsync(string userId, CancellationToken ct = default)
        => jobStatusHandler.GetUserJobsAsync(userId, ct);

    public Task<List<DownloadJob>> GetUserFilesAsync(string userId, CancellationToken ct = default)
        => listUserFilesHandler.GetFilesAsync(userId, ct);

    public Task DeleteFileAsync(Guid jobId, string userId, CancellationToken ct = default)
        => deleteFileHandler.DeleteAsync(jobId, userId, ct);

    public Task<UserStatsDto> GetUserStatsAsync(string userId, CancellationToken ct = default)
        => downloadStatsHandler.GetUserStatsAsync(userId, ct);

    public Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken ct = default)
        => downloadStatsHandler.GetGlobalStatsAsync(ct);
}
