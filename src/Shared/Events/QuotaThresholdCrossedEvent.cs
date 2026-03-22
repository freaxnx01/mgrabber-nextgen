namespace MusicGrabber.Shared.Events;

public sealed record QuotaThresholdCrossedEvent(
    string UserId,
    string Threshold);
