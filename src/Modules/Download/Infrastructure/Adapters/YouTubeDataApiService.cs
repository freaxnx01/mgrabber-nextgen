using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Download.Infrastructure.Adapters;

public sealed class YouTubeDataApiService(
    HttpClient httpClient,
    IMemoryCache cache,
    IConfiguration configuration,
    ILogger<YouTubeDataApiService> logger) : IYouTubeSearchService
{
    public async Task<List<YouTubeSearchResultDto>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default)
    {
        var key = $"yt:search:{query}:{maxResults}";
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await SearchYouTubeApiAsync(query, maxResults, ct);
        }) ?? [];
    }

    private async Task<List<YouTubeSearchResultDto>> SearchYouTubeApiAsync(string query, int maxResults, CancellationToken ct)
    {
        var apiKey = configuration["YOUTUBE_API_KEY"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("YouTube API key not configured");
            return [];
        }

        var url = $"https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&maxResults={maxResults}&q={Uri.EscapeDataString(query)}&key={apiKey}";
        var response = await httpClient.GetFromJsonAsync<JsonElement>(url, ct);

        if (!response.TryGetProperty("items", out var items))
            return [];

        return items.EnumerateArray().Select(item =>
        {
            var snippet = item.GetProperty("snippet");
            var videoId = item.GetProperty("id").GetProperty("videoId").GetString() ?? "";

            return new YouTubeSearchResultDto(
                VideoId: videoId,
                Title: snippet.GetProperty("title").GetString() ?? "",
                Author: snippet.GetProperty("channelTitle").GetString() ?? "",
                Duration: "",
                ThumbnailUrl: snippet.TryGetProperty("thumbnails", out var thumbs) &&
                    thumbs.TryGetProperty("medium", out var medium)
                    ? medium.GetProperty("url").GetString() ?? "" : ""
            );
        }).ToList();
    }
}
