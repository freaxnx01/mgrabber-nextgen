namespace MusicGrabber.Modules.Radio.Domain;

public sealed record RadioSong(
    string Artist,
    string Title,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime);
