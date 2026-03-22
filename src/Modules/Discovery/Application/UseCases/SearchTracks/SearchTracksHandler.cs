using MusicGrabber.Modules.Discovery.Application.Ports.Driven;
using MusicGrabber.Modules.Discovery.Domain;

namespace MusicGrabber.Modules.Discovery.Application.UseCases.SearchTracks;

public sealed class SearchTracksHandler(IMusicBrainzApi api)
{
    public Task<List<TrackResult>> SearchTracksAsync(string query, int limit = 10, CancellationToken ct = default)
        => api.SearchTracksAsync(query, limit, ct);
}
