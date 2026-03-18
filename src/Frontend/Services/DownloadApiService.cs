using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Frontend.Services;

public partial class DownloadApiService : IDownloadApiService
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

    // ========== Whitelist Management Methods ==========

    public async Task<List<WhitelistEntryDto>?> GetWhitelistAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/admin/whitelist");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<WhitelistEntryDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get whitelist");
            return null;
        }
    }

    public async Task<WhitelistEntryDto?> AddToWhitelistAsync(AddToWhitelistRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/admin/whitelist", request);
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException("User is already whitelisted");
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WhitelistEntryDto>();
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to add user to whitelist");
            throw;
        }
    }

    public async Task UpdateWhitelistStatusAsync(string id, bool isActive)
    {
        try
        {
            var request = new UpdateWhitelistRequestDto(IsActive: isActive);
            var response = await _httpClient.PutAsJsonAsync($"/api/admin/whitelist/{id}", request);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update whitelist status");
            throw;
        }
    }

    public async Task RemoveFromWhitelistAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/admin/whitelist/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove user from whitelist");
            throw;
        }
    }

    // ========== Quota Methods ==========

    public async Task<UserQuotaInfoDto?> GetUserQuotaAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/quota/{userId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserQuotaInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user quota");
            return null;
        }
    }

    // ========== Playlist Methods ==========

    public async Task<PlaylistInfoDto?> GetPlaylistInfoAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/playlist/info?url={Uri.EscapeDataString(url)}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlaylistInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get playlist info");
            return null;
        }
    }

    public async Task<List<PlaylistVideoDto>?> GetPlaylistVideosAsync(string playlistId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/playlist/videos?playlistId={Uri.EscapeDataString(playlistId)}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PlaylistVideosResponseDto>();
            return result?.Videos?.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get playlist videos");
            return null;
        }
    }

    public async Task<PlaylistDownloadResultDto?> StartPlaylistDownloadAsync(
        string playlistId,
        string userId,
        List<string> selectedVideoIds,
        string format,
        bool normalize)
    {
        try
        {
            var request = new
            {
                PlaylistId = playlistId,
                UserId = userId,
                SelectedVideoIds = selectedVideoIds,
                Format = format,
                Normalize = normalize
            };

            var response = await _httpClient.PostAsJsonAsync("/api/playlist/download", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlaylistDownloadResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start playlist download");
            return null;
        }
    }

    // ========== MusicBrainz Search Methods ==========

    public async Task<MusicBrainzSearchResultDto?> SearchMusicBrainzArtistsAsync(string query)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/search/musicbrainz?type=artist&q={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MusicBrainzSearchResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search MusicBrainz artists");
            return null;
        }
    }

    public async Task<MusicBrainzSearchResultDto?> SearchMusicBrainzTracksAsync(string query)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/search/musicbrainz?type=track&q={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MusicBrainzSearchResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search MusicBrainz tracks");
            return null;
        }
    }

    public async Task<MusicBrainzSearchResultDto?> SearchMusicBrainzReleasesAsync(string query)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/search/musicbrainz?type=album&q={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MusicBrainzSearchResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search MusicBrainz releases");
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

// Whitelist DTOs
public class WhitelistEntryDto
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string AddedBy { get; set; } = "";
    public DateTime AddedAt { get; set; }
    public bool IsActive { get; set; }
    public bool WelcomeEmailSent { get; set; }
}

public record AddToWhitelistRequestDto(string UserId, bool SendWelcomeEmail = false);
public record UpdateWhitelistRequestDto(bool IsActive);

// MusicBrainz DTOs
public class MusicBrainzSearchResultDto
{
    public string Query { get; set; } = "";
    public string Type { get; set; } = "";
    public List<MusicBrainzArtistDto> Artists { get; set; } = new();
    public List<MusicBrainzTrackDto> Tracks { get; set; } = new();
    public List<MusicBrainzReleaseDto> Releases { get; set; } = new();
    public int TotalCount { get; set; }
}

public class MusicBrainzArtistDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? SortName { get; set; }
    public string? Country { get; set; }
    public string? Type { get; set; }
    public string? Disambiguation { get; set; }
    public int? Score { get; set; }
    public List<string> Genres { get; set; } = new();
}

public class MusicBrainzTrackDto
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? FormattedDuration { get; set; }
    public int? Score { get; set; }
    public string? Artist { get; set; }
    public string? ArtistId { get; set; }
}

public class MusicBrainzReleaseDto
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Date { get; set; }
    public string? Country { get; set; }
    public int? TrackCount { get; set; }
    public int? Score { get; set; }
    public string? Artist { get; set; }
}

// Quota DTOs
public class UserQuotaInfoDto
{
    public string UserId { get; set; } = "";
    public long TotalBytesAllowed { get; set; }
    public long TotalBytesUsed { get; set; }
    public double PercentageUsed { get; set; }
    public long RemainingBytes { get; set; }
    public int FileCount { get; set; }
    public QuotaThreshold Threshold { get; set; }
    public double UsedMB => TotalBytesUsed / (1024.0 * 1024.0);
    public double TotalMB => TotalBytesAllowed / (1024.0 * 1024.0);
    public double RemainingMB => RemainingBytes / (1024.0 * 1024.0);
}

public enum QuotaThreshold
{
    Normal,
    Warning,
    Critical,
    Blocked
}

// User Profile DTOs
public class UserProfileDto
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime JoinedDate { get; set; }
}

public class UserSettingsDto
{
    public string DefaultFormat { get; set; } = "mp3";
    public bool EnableNormalization { get; set; }
    public int NormalizationLevel { get; set; } = -14;
    public bool EmailNotifications { get; set; } = true;
}

// Playlist DTOs
public class PlaylistInfoDto
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public int VideoCount { get; set; }
    public string? PublishedAt { get; set; }
}

public class PlaylistVideoDto
{
    public int Position { get; set; }
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string? PublishedAt { get; set; }
}

public class PlaylistVideosResponseDto
{
    public string PlaylistId { get; set; } = "";
    public int TotalVideos { get; set; }
    public List<PlaylistVideoDto> Videos { get; set; } = new();
}

public class PlaylistDownloadResultDto
{
    public string Message { get; set; } = "";
    public int TotalVideos { get; set; }
    public List<object> Jobs { get; set; } = new();
}
