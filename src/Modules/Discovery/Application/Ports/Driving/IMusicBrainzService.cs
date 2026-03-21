using MusicGrabber.Modules.Discovery.Domain;

namespace MusicGrabber.Modules.Discovery.Application.Ports.Driving;

public interface IMusicBrainzService
{
    Task<List<ArtistResult>> SearchArtistsAsync(string query, int limit = 10, CancellationToken ct = default);
    Task<List<TrackResult>> SearchTracksAsync(string query, int limit = 10, CancellationToken ct = default);
    Task<List<ReleaseResult>> SearchReleasesAsync(string query, int limit = 10, CancellationToken ct = default);
    Task<ArtistResult?> GetArtistDetailsAsync(string artistId, CancellationToken ct = default);
}
