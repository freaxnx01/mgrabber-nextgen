namespace MusicGrabber.Shared.DTOs;

public sealed record FileInfoDto(
    Guid JobId,
    string Title,
    string Author,
    string Format,
    string CorrectedFilename,
    long FileSizeBytes,
    DateTime CompletedAt);
