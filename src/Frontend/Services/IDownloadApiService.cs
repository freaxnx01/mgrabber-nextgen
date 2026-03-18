namespace Frontend.Services;

public interface IDownloadApiService
{
    Task<YouTubeSearchResult?> SearchYouTubeAsync(string query);
    Task<DownloadStartResult?> StartDownloadAsync(string url, string title, string? author = null);
    Task<DownloadStatus?> GetDownloadStatusAsync(string jobId);
    Task<List<DownloadItem>?> GetUserDownloadsAsync();
    Task<List<FileItem>?> GetUserFilesAsync();
    Task<bool> DeleteFileAsync(string jobId);
    Task<GlobalStatsDto?> GetGlobalStatsAsync();
    Task<List<UserStatsDto>?> GetAllUserStatsAsync();
    Task<UserDetailStatsDto?> GetUserDetailStatsAsync(string userId);
    Task<List<WhitelistEntryDto>?> GetWhitelistAsync();
    Task<WhitelistEntryDto?> AddToWhitelistAsync(AddToWhitelistRequestDto request);
    Task UpdateWhitelistStatusAsync(string id, bool isActive);
    Task RemoveFromWhitelistAsync(string id);
    Task<UserQuotaInfoDto?> GetUserQuotaAsync(string userId);
    Task<PlaylistInfoDto?> GetPlaylistInfoAsync(string url);
    Task<List<PlaylistVideoDto>?> GetPlaylistVideosAsync(string playlistId);
    Task<PlaylistDownloadResultDto?> StartPlaylistDownloadAsync(string playlistId, string userId, List<string> selectedVideoIds, string format, bool normalize);
    Task<MusicBrainzSearchResultDto?> SearchMusicBrainzArtistsAsync(string query);
    Task<MusicBrainzSearchResultDto?> SearchMusicBrainzTracksAsync(string query);
    Task<MusicBrainzSearchResultDto?> SearchMusicBrainzReleasesAsync(string query);
    
    // Radio methods
    Task<List<RadioStationDto>?> GetRadioStationsAsync();
    Task<RadioPlaylistResponseDto?> GetRadioPlaylistAsync(string station, int limit = 20);
    Task<RadioNowPlayingDto?> GetRadioNowPlayingAsync(string station);
    Task<RadioDownloadResultDto?> DownloadRadioSongAsync(string station, string userId, string format, bool autoSelectBestMatch = true);
}
