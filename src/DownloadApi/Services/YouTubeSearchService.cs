using System.Net.Http.Json;

namespace DownloadApi.Services;

public interface IYouTubeSearchService
{
    Task<YouTubeSearchResult> SearchAsync(string query, int maxResults = 10);
}

public class YouTubeSearchService : IYouTubeSearchService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<YouTubeSearchService> _logger;
    private readonly IMemoryCache _cache;

    public YouTubeSearchService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<YouTubeSearchService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
    }

    public async Task<YouTubeSearchResult> SearchAsync(string query, int maxResults = 10)
    {
        // Check cache first
        var cacheKey = $"youtube_search_{query}_{maxResults}";
        if (_cache.TryGetValue(cacheKey, out YouTubeSearchResult? cachedResult) && cachedResult != null)
        {
            _logger.LogDebug("Returning cached YouTube search results for: {Query}", query);
            return cachedResult;
        }

        var apiKey = _configuration["YouTube:ApiKey"] 
            ?? throw new InvalidOperationException("YouTube API Key not configured");

        try
        {
            var response = await _httpClient.GetAsync(
                $"search?part=snippet&q={Uri.EscapeDataString(query)}&type=video&maxResults={maxResults}&key={apiKey}");

            response.EnsureSuccessStatusCode();
            var youtubeResponse = await response.Content.ReadFromJsonAsync<YouTubeApiResponse>();

            var results = youtubeResponse?.Items?.Select(item => new YouTubeVideo
            {
                VideoId = item.Id?.VideoId ?? "",
                Title = item.Snippet?.Title ?? "",
                Author = item.Snippet?.ChannelTitle ?? "",
                Duration = "", // Would need separate API call for duration
                ThumbnailUrl = item.Snippet?.Thumbnails?.Medium?.Url ?? ""
            }).Where(v => !string.IsNullOrEmpty(v.VideoId)).ToList() ?? new List<YouTubeVideo>();

            var result = new YouTubeSearchResult
            {
                Query = query,
                Results = results,
                TotalResults = youtubeResponse?.PageInfo?.TotalResults ?? 0
            };

            // Cache for 5 minutes to reduce API quota usage
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

            _logger.LogInformation("YouTube search for '{Query}' returned {Count} results", query, results.Count);
            return result;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogError(ex, "YouTube API quota exceeded or invalid API key");
            throw new InvalidOperationException("YouTube API error. Please check your API key and quota.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search YouTube for: {Query}", query);
            throw;
        }
    }
}

// DTOs for YouTube API
public class YouTubeSearchResult
{
    public string Query { get; set; } = "";
    public List<YouTubeVideo> Results { get; set; } = new();
    public int TotalResults { get; set; }
}

public class YouTubeVideo
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Duration { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
}

// YouTube API Response Models
internal class YouTubeApiResponse
{
    public string? Kind { get; set; }
    public string? Etag { get; set; }
    public string? NextPageToken { get; set; }
    public string? RegionCode { get; set; }
    public PageInfo? PageInfo { get; set; }
    public List<SearchItem>? Items { get; set; }
}

internal class PageInfo
{
    public int TotalResults { get; set; }
    public int ResultsPerPage { get; set; }
}

internal class SearchItem
{
    public string? Kind { get; set; }
    public string? Etag { get; set; }
    public ItemId? Id { get; set; }
    public Snippet? Snippet { get; set; }
}

internal class ItemId
{
    public string? Kind { get; set; }
    public string? VideoId { get; set; }
}

internal class Snippet
{
    public string? PublishedAt { get; set; }
    public string? ChannelId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ChannelTitle { get; set; }
    public string? LiveBroadcastContent { get; set; }
    public Thumbnails? Thumbnails { get; set; }
}

internal class Thumbnails
{
    public Thumbnail? Default { get; set; }
    public Thumbnail? Medium { get; set; }
    public Thumbnail? High { get; set; }
}

internal class Thumbnail
{
    public string? Url { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}
