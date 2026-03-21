namespace MusicGrabber.Shared.DTOs;

public sealed record StartDownloadRequest(
    string Url,
    string UserId,
    string Format,
    string? Title = null,
    string? Author = null,
    bool NormalizeAudio = false,
    int NormalizationLufs = -14);
