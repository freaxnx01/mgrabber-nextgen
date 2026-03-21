namespace MusicGrabber.Shared.DTOs;

public sealed record UserStatsDto(
    string UserId,
    int TotalDownloads,
    int CompletedDownloads,
    int FailedDownloads,
    long TotalStorageBytes,
    DateTime? LastActive,
    IReadOnlyDictionary<string, int> TopArtists,
    IReadOnlyDictionary<string, int> DownloadsPerDay);
