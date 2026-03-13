using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Frontend.Services;

public class DownloadApiService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DownloadApiService> _logger;

    public DownloadApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<DownloadApiService> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
    }

    // Search YouTube
    public async Task<YouTubeSearchResult?> SearchYouTubeAsync(string query)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/search/youtube?q={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<YouTubeSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search YouTube");
            return null;
        }
    }

    // Start download
    public async Task<DownloadStartResult?> StartDownloadAsync(string url, string title, string? author = null)
    {
        try
        {
            var request = new
            {
                Url = url,
                UserId = GetCurrentUserId(),
                Title = title,
                Author = author
            };

            var response = await _httpClient.PostAsJsonAsync("/api/download/start", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DownloadStartResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start download");
            return null;
        }
    }

    // Get download status
    public async Task<DownloadStatus?> GetDownloadStatusAsync(string jobId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/download/status/{jobId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DownloadStatus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get download status");
            return null;
        }
    }

    // Get user's downloads
    public async Task<List<DownloadItem>?> GetUserDownloadsAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _httpClient.GetAsync($"/api/jobs/{userId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<DownloadItem>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user downloads");
            return null;
        }
    }

    // Get user's files
    public async Task<List<FileItem>?> GetUserFilesAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _httpClient.GetAsync($"/api/files/{userId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<FileItem>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user files");
            return null;
        }
    }

    // Delete file
    public async Task<bool> DeleteFileAsync(string jobId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _httpClient.DeleteAsync($"/api/files/{userId}/{jobId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file");
            return false;
        }
    }

    // ========== Admin Methods ==========

    public async Task<GlobalStatsDto?> GetGlobalStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/admin/stats/global");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GlobalStatsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get global stats");
            return null;
        }
    }

    public async Task<List<UserStatsDto>?> GetAllUserStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/admin/stats/users");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<UserStatsDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user stats");
            return null;
        }
    }

    public async Task<UserDetailStatsDto?> GetUserDetailStatsAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/admin/stats/users/{userId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserDetailStatsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user detail stats");
            return null;
        }
    }
}

// DTOs
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

public class DownloadStartResult
{
    public string JobId { get; set; } = "";
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
}

public class DownloadStatus
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string Format { get; set; } = "";
    public string Status { get; set; } = "";
    public int Progress { get; set; }
    public string? FilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class DownloadItem
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string Status { get; set; } = "";
    public int Progress { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FileItem
{
    public string Id { get; set; } = "";
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string Format { get; set; } = "";
    public double FileSizeMB { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Admin DTOs
public class GlobalStatsDto
{
    public int TotalDownloads { get; set; }
    public long TotalStorageBytes { get; set; }
    public double TotalStorageMB { get; set; }
    public List<DailyDownloadCountDto> DownloadsPerDay { get; set; } = new();
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public int ActiveUsersLast7Days { get; set; }
}

public class DailyDownloadCountDto
{
    public string Date { get; set; } = "";
    public int Count { get; set; }
}

public class UserStatsDto
{
    public string UserId { get; set; } = "";
    public int TotalDownloads { get; set; }
    public double TotalStorageMB { get; set; }
    public int CompletedDownloads { get; set; }
    public int FailedDownloads { get; set; }
    public DateTime? LastActive { get; set; }
}

public class UserDetailStatsDto : UserStatsDto
{
    public List<ArtistCountDto> TopArtists { get; set; } = new();
    public List<DailyDownloadCountDto> DownloadsPerDay { get; set; } = new();
}

public class ArtistCountDto
{
    public string Artist { get; set; } = "";
    public int Count { get; set; }
}
