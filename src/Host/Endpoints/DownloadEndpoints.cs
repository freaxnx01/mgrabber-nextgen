using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Application.UseCases.DeleteFile;
using MusicGrabber.Modules.Download.Application.UseCases.GetDownloadStats;
using MusicGrabber.Modules.Download.Application.UseCases.GetJobStatus;
using MusicGrabber.Modules.Download.Application.UseCases.ListUserFiles;
using MusicGrabber.Modules.Download.Application.UseCases.SearchYouTube;
using MusicGrabber.Modules.Download.Application.UseCases.StartDownload;
using MusicGrabber.Modules.Download.Application.UseCases.StartPlaylistDownload;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Host.Endpoints;

public static class DownloadEndpoints
{
    public static void MapDownloadEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/youtube/search", async (string q, SearchYouTubeHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.SearchAsync(q, 10, ct)));

        group.MapPost("/downloads", async (StartDownloadRequest request, StartDownloadHandler handler, CancellationToken ct) =>
        {
            var jobId = await handler.StartAsync(request, ct);
            return Results.Created($"/api/v1/downloads/{jobId}/status", new { JobId = jobId });
        });

        group.MapGet("/downloads/{id:guid}/status", async (Guid id, GetJobStatusHandler handler, CancellationToken ct) =>
        {
            var job = await handler.GetAsync(id, ct);
            return job is not null ? Results.Ok(job) : Results.NotFound();
        });

        group.MapGet("/downloads/users/{userId}", async (string userId, GetJobStatusHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.GetUserJobsAsync(userId, ct)));

        group.MapGet("/downloads/users/{userId}/stats", async (string userId,
            GetDownloadStatsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.GetUserStatsAsync(userId, ct)));

        group.MapGet("/playlists/info", async (string url, IAudioExtractor extractor, CancellationToken ct) =>
        {
            var info = await extractor.GetInfoAsync(url, ct);
            return Results.Ok(info);
        });

        // Errata #19: playlist videos endpoint
        group.MapGet("/playlists/videos", async (string playlistId, IAudioExtractor extractor, CancellationToken ct) =>
        {
            var info = await extractor.GetInfoAsync($"https://www.youtube.com/playlist?list={playlistId}", ct);
            return Results.Ok(info);
        });

        group.MapPost("/playlists/download", async (PlaylistDownloadRequest request,
            StartPlaylistDownloadHandler handler, CancellationToken ct) =>
        {
            var jobIds = await handler.StartPlaylistAsync(
                request.PlaylistId, request.UserId, request.VideoUrls,
                request.Format, request.Normalize, ct);
            return Results.Created("/api/v1/downloads", new { JobIds = jobIds });
        });

        group.MapGet("/files/users/{userId}", async (string userId, ListUserFilesHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.GetFilesAsync(userId, ct)));

        group.MapGet("/files/{id:guid}/download", async (Guid id, GetJobStatusHandler handler, CancellationToken ct) =>
        {
            var job = await handler.GetAsync(id, ct);
            if (job?.FilePath is null || !File.Exists(job.FilePath))
                return Results.NotFound();

            var contentType = job.Format.ToString().ToLowerInvariant() switch
            {
                "mp3" => "audio/mpeg",
                "flac" => "audio/flac",
                "m4a" => "audio/mp4",
                "webm" => "audio/webm",
                _ => "application/octet-stream"
            };
            return Results.File(job.FilePath, contentType, job.CorrectedFilename ?? Path.GetFileName(job.FilePath));
        });

        group.MapDelete("/files/{id:guid}", async (Guid id, string userId, DeleteFileHandler handler, CancellationToken ct) =>
        {
            await handler.DeleteAsync(id, userId, ct);
            return Results.NoContent();
        });
    }
}

public sealed record PlaylistDownloadRequest(
    string PlaylistId, string UserId, List<string> VideoUrls,
    string Format, bool Normalize);
