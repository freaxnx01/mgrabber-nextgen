using System.Text.RegularExpressions;

namespace DownloadApi.Services;

public interface IPlaylistService
{
    Task<PlaylistInfo> GetPlaylistInfoAsync(string playlistUrl);
    Task<List<PlaylistVideo>> GetPlaylistVideosAsync(string playlistId);
}

public class YouTubePlaylistService : IPlaylistService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YouTubePlaylistService> _logger;
    private readonly string _apiKey;

    public YouTubePlaylistService(HttpClient httpClient, IConfiguration configuration, ILogger<YouTubePlaylistService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["YouTube:ApiKey"] ?? throw new InvalidOperationException("YouTube API Key not configured");
        _httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
    }

    public async Task<PlaylistInfo> GetPlaylistInfoAsync(string playlistUrl)
    {
        var playlistId = ExtractPlaylistId(playlistUrl);
        if (string.IsNullOrEmpty(playlistId))
        {
            throw new ArgumentException("Invalid YouTube playlist URL");
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"playlists?part=snippet,contentDetails&id={playlistId}&key={_apiKey}");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<YouTubePlaylistResponse>();

            var playlist = result?.Items?.FirstOrDefault();
            if (playlist == null)
            {
                throw new Exception("Playlist not found");
            }

            return new PlaylistInfo
            {
                Id = playlistId,
                Title = playlist.Snippet?.Title ?? "Unknown Playlist",
                Description = playlist.Snippet?.Description ?? "",
                Author = playlist.Snippet?.ChannelTitle ?? "Unknown Channel",
                ThumbnailUrl = playlist.Snippet?.Thumbnails?.Medium?.Url ?? "",
                VideoCount = playlist.ContentDetails?.ItemCount ?? 0,
                PublishedAt = playlist.Snippet?.PublishedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get playlist info for {Url}", playlistUrl);
            throw;
        }
    }

    public async Task<List<PlaylistVideo>> GetPlaylistVideosAsync(string playlistId)
    {
        var videos = new List<PlaylistVideo>();
        string? nextPageToken = null;

        try
        {
            do
            {
                var url = $"playlistItems?part=snippet,contentDetails&playlistId={playlistId}&maxResults=50&key={_apiKey}";
                if (!string.IsNullOrEmpty(nextPageToken))
                {
                    url += $"&pageToken={nextPageToken}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<YouTubePlaylistItemsResponse>();
                
                if (result?.Items != null)
                {
                    foreach (var item in result.Items)
                    {
                        if (item.Snippet?.ResourceId?.VideoId != null)
                        {
                            videos.Add(new PlaylistVideo
                            {
                                Position = item.Snippet.Position ?? 0,
                                VideoId = item.Snippet.ResourceId.VideoId,
                                Title = item.Snippet.Title ?? "Unknown Title",
                                Author = item.Snippet.VideoOwnerChannelTitle ?? "Unknown Channel",
                                ThumbnailUrl = item.Snippet.Thumbnails?.Medium?.Url ?? "",
                                PublishedAt = item.Snippet.PublishedAt
                            });
                        }
                    }
                }

                nextPageToken = result?.NextPageToken;

            } while (!string.IsNullOrEmpty(nextPageToken));

            _logger.LogInformation("Retrieved {Count} videos from playlist {PlaylistId}", videos.Count, playlistId);
            return videos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get playlist videos for {PlaylistId}", playlistId);
            throw;
        }
    }

    public static string? ExtractPlaylistId(string url)
    {
        // Handle various YouTube playlist URL formats
        var patterns = new[]
        {
            @"[?&]list=([a-zA-Z0-9_-]+)",
            @"youtube\.com/playlist\?list=([a-zA-Z0-9_-]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(url, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }
}

public class PlaylistInfo
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public int VideoCount { get; set; }
    public string? PublishedAt { get; set; }
}

public class PlaylistVideo
{
    public int Position { get; set; }
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string? PublishedAt { get; set; }
}

// YouTube API Response Models
internal class YouTubePlaylistResponse
{
    public List<PlaylistItem>? Items { get; set; }
}

internal class PlaylistItem
{
    public string? Id { get; set; }
    public PlaylistSnippet? Snippet { get; set; }
    public PlaylistContentDetails? ContentDetails { get; set; }
}

internal class PlaylistSnippet
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ChannelTitle { get; set; }
    public string? PublishedAt { get; set; }
    public ThumbnailSet? Thumbnails { get; set; }
}

internal class PlaylistContentDetails
{
    public int? ItemCount { get; set; }
}

internal class ThumbnailSet
{
    public ThumbnailInfo? Medium { get; set; }
}

internal class ThumbnailInfo
{
    public string? Url { get; set; }
}

internal class YouTubePlaylistItemsResponse
{
    public List<PlaylistItemDetail>? Items { get; set; }
    public string? NextPageToken { get; set; }
}

internal class PlaylistItemDetail
{
    public string? Id { get; set; }
    public PlaylistItemSnippet? Snippet { get; set; }
}

internal class PlaylistItemSnippet
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ChannelTitle { get; set; }
    public string? VideoOwnerChannelTitle { get; set; }
    public string? PublishedAt { get; set; }
    public int? Position { get; set; }
    public ResourceId? ResourceId { get; set; }
    public ThumbnailSet? Thumbnails { get; set; }
}

internal class ResourceId
{
    public string? VideoId { get; set; }
}
