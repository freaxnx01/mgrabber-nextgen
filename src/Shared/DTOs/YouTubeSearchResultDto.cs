namespace MusicGrabber.Shared.DTOs;

public sealed record YouTubeSearchResultDto(
    string VideoId,
    string Title,
    string Author,
    string Duration,
    string ThumbnailUrl);
