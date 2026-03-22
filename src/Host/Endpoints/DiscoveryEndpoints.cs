using MusicGrabber.Modules.Discovery.Application.Ports.Driving;

namespace MusicGrabber.Host.Endpoints;

public static class DiscoveryEndpoints
{
    public static void MapDiscoveryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/musicbrainz").RequireAuthorization();

        group.MapGet("/search", async (string type, string q, IMusicBrainzService service, CancellationToken ct) =>
        {
            return type.ToLowerInvariant() switch
            {
                "artist" => Results.Ok(await service.SearchArtistsAsync(q, 10, ct)),
                "track" => Results.Ok(await service.SearchTracksAsync(q, 10, ct)),
                "release" => Results.Ok(await service.SearchReleasesAsync(q, 10, ct)),
                _ => Results.BadRequest("Invalid type. Use: artist, track, release")
            };
        });

        group.MapGet("/artists/{id}", async (string id, IMusicBrainzService service, CancellationToken ct) =>
        {
            var artist = await service.GetArtistDetailsAsync(id, ct);
            return artist is not null ? Results.Ok(artist) : Results.NotFound();
        });
    }
}
