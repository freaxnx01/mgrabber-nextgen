namespace MusicGrabber.Shared.DTOs;

public sealed record QuotaInfoDto(
    string UserId,
    long QuotaBytes,
    long UsedBytes,
    int FileCount,
    string CurrentThreshold);
