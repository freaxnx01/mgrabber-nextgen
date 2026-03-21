using MusicGrabber.Modules.Discovery.Application.Ports.Driven;
using MusicGrabber.Modules.Discovery.Application.Ports.Driving;
using MusicGrabber.Modules.Discovery.Domain;

namespace MusicGrabber.Modules.Discovery.Application.Services;

public sealed class MusicBrainzService(IMusicBrainzApi api) : IMusicBrainzService
{
    public Task<List<ArtistResult>> SearchArtistsAsync(string query, int limit = 10, CancellationToken ct = default)
        => api.SearchArtistsAsync(query, limit, ct);

    public Task<List<TrackResult>> SearchTracksAsync(string query, int limit = 10, CancellationToken ct = default)
        => api.SearchTracksAsync(query, limit, ct);

    public Task<List<ReleaseResult>> SearchReleasesAsync(string query, int limit = 10, CancellationToken ct = default)
        => api.SearchReleasesAsync(query, limit, ct);

    public Task<ArtistResult?> GetArtistDetailsAsync(string artistId, CancellationToken ct = default)
        => api.GetArtistDetailsAsync(artistId, ct);
}
