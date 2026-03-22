using MusicGrabber.Modules.Radio.Application.Ports.Driving;
using MusicGrabber.Shared.Contracts;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Host.Endpoints;

public static class RadioEndpoints
{
    public static void MapRadioEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/radio").RequireAuthorization();

        group.MapGet("/stations", async (IRadioService service, CancellationToken ct) =>
            Results.Ok(await service.GetStationsAsync(ct)));

        group.MapGet("/stations/{id}/now-playing", async (string id, IRadioService service, CancellationToken ct) =>
        {
            var song = await service.GetNowPlayingAsync(id, ct);
            return song is not null ? Results.Ok(song) : Results.NotFound();
        });

        group.MapGet("/stations/{id}/playlist", async (string id, int? limit, IRadioService service, CancellationToken ct) =>
            Results.Ok(await service.GetPlaylistAsync(id, limit ?? 10, ct)));

        group.MapPost("/download", async (RadioDownloadRequest request, IDownloadFacade downloadFacade, CancellationToken ct) =>
        {
            var downloadRequest = new StartDownloadRequest(
                Url: $"ytsearch1:{request.Artist} {request.Title}",
                UserId: request.UserId,
                Format: request.Format,
                Title: request.Title,
                Author: request.Artist,
                NormalizeAudio: request.NormalizeAudio,
                NormalizationLufs: request.NormalizationLufs);

            var jobId = await downloadFacade.StartAsync(downloadRequest, ct);
            return Results.Created($"/api/v1/downloads/{jobId}/status", new { JobId = jobId });
        });
    }
}
