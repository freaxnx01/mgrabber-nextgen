using MusicGrabber.Modules.Discovery.Domain;

namespace MusicGrabber.Frontend.Services;

public interface IDiscoveryFrontendService
{
    Task<List<ArtistResult>> SearchArtistsAsync(string query, int limit = 10, CancellationToken ct = default);
    Task<List<TrackResult>> SearchTracksAsync(string query, int limit = 10, CancellationToken ct = default);
    Task<List<ReleaseResult>> SearchReleasesAsync(string query, int limit = 10, CancellationToken ct = default);
}
