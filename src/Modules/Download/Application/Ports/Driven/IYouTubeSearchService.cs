using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Download.Application.Ports.Driven;

public interface IYouTubeSearchService
{
    Task<List<YouTubeSearchResultDto>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default);
}
