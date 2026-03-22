namespace MusicGrabber.Shared.DTOs;

public sealed record GlobalStatsDto(
    int TotalDownloads,
    long TotalStorageBytes,
    int ActiveUsersLast7Days,
    IReadOnlyDictionary<string, int> StatusCounts,
    IReadOnlyDictionary<string, int> DownloadsPerDay,
    IReadOnlyList<UserStatsDto> Users);
