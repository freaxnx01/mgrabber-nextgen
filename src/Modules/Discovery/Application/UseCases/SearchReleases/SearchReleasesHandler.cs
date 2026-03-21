using MusicGrabber.Modules.Discovery.Application.Ports.Driven;
using MusicGrabber.Modules.Discovery.Domain;

namespace MusicGrabber.Modules.Discovery.Application.UseCases.SearchReleases;

public sealed class SearchReleasesHandler(IMusicBrainzApi api)
{
    public Task<List<ReleaseResult>> SearchReleasesAsync(string query, int limit = 10, CancellationToken ct = default)
        => api.SearchReleasesAsync(query, limit, ct);
}
