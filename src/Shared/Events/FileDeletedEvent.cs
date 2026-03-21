namespace MusicGrabber.Shared.Events;

public sealed record FileDeletedEvent(
    Guid JobId,
    string UserId,
    long FileSizeBytes);
