namespace MusicGrabber.Shared.Events;

public sealed record DownloadCompletedEvent(
    Guid JobId,
    string UserId,
    long FileSizeBytes);
