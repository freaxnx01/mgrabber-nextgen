
namespace Frontend.Services;

// YouTube video result for radio search
public class YouTubeVideoDto
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Duration { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
}

// Radio DTOs
public class RadioStationDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "";
}

public class RadioStationsResponseDto
{
    public List<RadioStationDto> Stations { get; set; } = new();
}

public class RadioSongDto
{
    public string Artist { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime PlayedAt { get; set; }
    public string FormattedDuration { get; set; } = "";
    public bool IsPlayingNow { get; set; }
    public string Station { get; set; } = "";
    public string StationName { get; set; } = "";
}

public class RadioPlaylistResponseDto
{
    public string Station { get; set; } = "";
    public RadioSongDto? NowPlaying { get; set; }
    public List<RadioSongDto> Songs { get; set; } = new();
}

public class RadioNowPlayingDto
{
    public string Artist { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime PlayedAt { get; set; }
    public string FormattedDuration { get; set; } = "";
    public bool IsPlayingNow { get; set; }
    public string Station { get; set; } = "";
    public string StationName { get; set; } = "";
    public string SearchQuery { get; set; } = "";
}

public class RadioDownloadResultDto
{
    public bool Success { get; set; }
    public RadioSongDto? Song { get; set; }
    public object? Download { get; set; }
    public List<YouTubeVideoDto>? YouTubeResults { get; set; }
    public string? Message { get; set; }
}
