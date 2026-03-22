namespace MusicGrabber.Shared.DTOs;

public sealed record RadioDownloadRequest(
    string StationId,
    string Artist,
    string Title,
    string UserId,
    string Format = "Mp3",
    bool NormalizeAudio = false,
    int NormalizationLufs = -14);
