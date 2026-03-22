namespace MusicGrabber.Modules.Discovery.Domain;

public sealed record ArtistResult(
    string Id, string Name, string? SortName, string? Country,
    string? Type, string? Disambiguation, int Score);

public sealed record TrackResult(
    string Id, string Title, string? FormattedDuration,
    int Score, string? ArtistCredit);

public sealed record ReleaseResult(
    string Id, string Title, string? Date, string? Country,
    int Score, string? ArtistCredit);
