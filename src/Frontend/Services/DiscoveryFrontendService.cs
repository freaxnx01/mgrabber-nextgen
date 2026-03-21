using MusicGrabber.Modules.Discovery.Application.Ports.Driving;
using MusicGrabber.Modules.Discovery.Domain;

namespace MusicGrabber.Frontend.Services;

public sealed class DiscoveryFrontendService(IMusicBrainzService musicBrainzService) : IDiscoveryFrontendService
{
    public Task<List<ArtistResult>> SearchArtistsAsync(string query, int limit, CancellationToken ct)
        => musicBrainzService.SearchArtistsAsync(query, limit, ct);

    public Task<List<TrackResult>> SearchTracksAsync(string query, int limit, CancellationToken ct)
        => musicBrainzService.SearchTracksAsync(query, limit, ct);

    public Task<List<ReleaseResult>> SearchReleasesAsync(string query, int limit, CancellationToken ct)
        => musicBrainzService.SearchReleasesAsync(query, limit, ct);
}
