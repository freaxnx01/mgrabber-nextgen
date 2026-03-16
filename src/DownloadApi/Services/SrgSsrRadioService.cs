namespace DownloadApi.Services;

public interface IRadioService
{
    Task<List<RadioStation>> GetStationsAsync();
    Task<List<RadioSong>> GetPlaylistAsync(string stationId, int limit = 20);
    Task<RadioSong?> GetNowPlayingAsync(string stationId);
    Task<RadioStation?> ResolveStationAsync(string stationIdentifier);
}

public class SrgSsrRadioService : IRadioService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SrgSsrRadioService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _baseUrl;

    public SrgSsrRadioService(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<SrgSsrRadioService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        _baseUrl = configuration["Radio:SrgSsr:BaseUrl"] ?? "https://il.srgssr.ch/integrationlayer/2.0/srf";
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            configuration["Radio:SrgSsr:UserAgent"] ?? "MusicGrabber/1.0");
    }

    public async Task<List<RadioStation>> GetStationsAsync()
    {
        var cacheKey = "radio_stations";
        if (_cache.TryGetValue(cacheKey, out List<RadioStation>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            // Try to fetch from SRG SSR API
            var response = await _httpClient.GetAsync($"{_baseUrl}/channelList/radio");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SrgChannelListResponse>();
                var stations = result?.Channels?.Select(c => new RadioStation
                {
                    Id = MapChannelId(c.Id),
                    Name = c.Name,
                    ChannelId = c.Id,
                    Provider = "srgssr"
                }).ToList() ?? new List<RadioStation>();

                // Cache for 24 hours
                _cache.Set(cacheKey, stations, TimeSpan.FromHours(24));
                return stations;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch SRG SSR channel list, using defaults");
        }

        // Fallback to known stations
        var defaultStations = new List<RadioStation>
        {
            new() { Id = "srf1", Name = "Radio SRF 1", ChannelId = "69e8ac16-4327-4af4-b873-fd5cd6e895a7", Provider = "srgssr" },
            new() { Id = "srf2", Name = "Radio SRF 2 Kultur", ChannelId = "", Provider = "srgssr" },
            new() { Id = "srf3", Name = "Radio SRF 3", ChannelId = "", Provider = "srgssr" },
            new() { Id = "srf4", Name = "Radio SRF 4 News", ChannelId = "", Provider = "srgssr" },
            new() { Id = "srfvirus", Name = "Radio SRF Virus", ChannelId = "", Provider = "srgssr" },
            new() { Id = "srfmusikwelle", Name = "Radio SRF Musikwelle", ChannelId = "", Provider = "srgssr" }
        };

        _cache.Set(cacheKey, defaultStations, TimeSpan.FromHours(24));
        return defaultStations;
    }

    public async Task<List<RadioSong>> GetPlaylistAsync(string stationId, int limit = 20)
    {
        var station = await ResolveStationAsync(stationId);
        if (station?.ChannelId == null)
        {
            throw new ArgumentException($"Unknown station: {stationId}");
        }

        var cacheKey = $"radio_playlist_{stationId}";
        if (_cache.TryGetValue(cacheKey, out List<RadioSong>? cached) && cached != null)
        {
            return cached.Take(limit).ToList();
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/songList/radio/byChannel/{station.ChannelId}");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SrgSongListResponse>();

            var songs = result?.SongList?.Select(s => new RadioSong
            {
                Artist = s.Artist?.Name ?? "Unknown Artist",
                Title = NormalizeTitle(s.Title),
                PlayedAt = DateTime.Parse(s.Date),
                Duration = s.Duration,
                IsPlayingNow = s.IsPlayingNow,
                Station = stationId,
                StationName = station.Name
            }).ToList() ?? new List<RadioSong>();

            // Cache for 30 seconds
            _cache.Set(cacheKey, songs, TimeSpan.FromSeconds(30));
            return songs.Take(limit).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get playlist for station {StationId}", stationId);
            throw;
        }
    }

    public async Task<RadioSong?> GetNowPlayingAsync(string stationId)
    {
        var playlist = await GetPlaylistAsync(stationId, limit = 5);
        return playlist.FirstOrDefault(s => s.IsPlayingNow) ?? playlist.FirstOrDefault();
    }

    public async Task<RadioStation?> ResolveStationAsync(string stationIdentifier)
    {
        var stations = await GetStationsAsync();
        return stations.FirstOrDefault(s => 
            s.Id.Equals(stationIdentifier, StringComparison.OrdinalIgnoreCase) ||
            s.Name.Replace(" ", "").Equals(stationIdentifier, StringComparison.OrdinalIgnoreCase));
    }

    private string MapChannelId(string? channelId)
    {
        // Map known channel IDs to friendly identifiers
        return channelId switch
        {
            "69e8ac16-4327-4af4-b873-fd5cd6e895a7" => "srf1",
            _ => channelId?.ToLowerInvariant() ?? "unknown"
        };
    }

    private string NormalizeTitle(string? title)
    {
        if (string.IsNullOrEmpty(title)) return "Unknown Title";
        
        // SRG SSR returns titles in UPPERCASE, convert to Title Case
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title.ToLowerInvariant());
    }
}

public class RadioStation
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ChannelId { get; set; }
    public string Provider { get; set; } = "srgssr";
}

public class RadioSong
{
    public string Artist { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime PlayedAt { get; set; }
    public int Duration { get; set; } // milliseconds
    public bool IsPlayingNow { get; set; }
    public string Station { get; set; } = "";
    public string StationName { get; set; } = "";

    public string FormattedDuration => TimeSpan.FromMilliseconds(Duration).ToString(@"mm\:ss");
    public string SearchQuery => $"{Artist} {Title} official audio";
}

// SRG SSR API Response Models
internal class SrgChannelListResponse
{
    public List<SrgChannel>? Channels { get; set; }
}

internal class SrgChannel
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

internal class SrgSongListResponse
{
    public string? Next { get; set; }
    public List<SrgSong>? SongList { get; set; }
}

internal class SrgSong
{
    public bool IsPlayingNow { get; set; }
    public string? Date { get; set; }
    public int Duration { get; set; }
    public string? Title { get; set; }
    public SrgArtist? Artist { get; set; }
}

internal class SrgArtist
{
    public string? Name { get; set; }
}
