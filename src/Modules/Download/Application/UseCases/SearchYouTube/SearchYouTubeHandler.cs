using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Download.Application.UseCases.SearchYouTube;

public sealed class SearchYouTubeHandler(IYouTubeSearchService youTubeSearch)
{
    public Task<List<YouTubeSearchResultDto>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default)
        => youTubeSearch.SearchAsync(query, maxResults, ct);
}
