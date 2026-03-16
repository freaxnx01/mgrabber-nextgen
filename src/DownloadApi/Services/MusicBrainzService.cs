using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Caching.Memory;

namespace DownloadApi.Services;

public interface IMusicBrainzService
{
    Task<MusicBrainzSearchResult> SearchArtistsAsync(string query, int limit = 10);
    Task<MusicBrainzSearchResult> SearchTracksAsync(string query, int limit = 10);
    Task<MusicBrainzSearchResult> SearchReleasesAsync(string query, int limit = 10);
    Task<MusicBrainzArtist?> GetArtistDetailsAsync(string artistId);
}

public class MusicBrainzService : IMusicBrainzService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MusicBrainzService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly string UserAgent = "MusicGrabber/1.0 ( mgrabber@freaxnx01.ch )";
    private static readonly TimeSpan RequestDelay = TimeSpan.FromMilliseconds(1100); // Rate limit: 1 req/sec
    private static DateTime _lastRequestTime = DateTime.MinValue;

    public MusicBrainzService(
        HttpClient httpClient, 
        ILogger<MusicBrainzService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        _httpClient.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task RateLimitAsync()
    {
        var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
        if (timeSinceLastRequest < RequestDelay)
        {
            var delay = RequestDelay - timeSinceLastRequest;
            await Task.Delay(delay);
        }
        _lastRequestTime = DateTime.UtcNow;
    }

    public async Task<MusicBrainzSearchResult> SearchArtistsAsync(string query, int limit = 10)
    {
        var cacheKey = $"mb_artist_{query}_{limit}";
        if (_cache.TryGetValue(cacheKey, out MusicBrainzSearchResult? cached) && cached != null)
        {
            return cached;
        }

        await RateLimitAsync();

        try
        {
            var encodedQuery = HttpUtility.UrlEncode(query);
            var response = await _httpClient.GetAsync(
                $"artist?query={encodedQuery}&fmt=json&limit={limit}");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<MusicBrainzSearchResponse>(json);

            var result = new MusicBrainzSearchResult
            {
                Query = query,
                Type = "artist",
                Artists = searchResult?.Artists?.Select(a => new MusicBrainzArtist
                {
                    Id = a.Id,
                    Name = a.Name,
                    SortName = a.SortName,
                    Country = a.Country,
                    Type = a.Type,
                    Disambiguation = a.Disambiguation,
                    Score = a.Score
                }).ToList() ?? new List<MusicBrainzArtist>(),
                TotalCount = searchResult?.Count ?? 0
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            _logger.LogInformation("MusicBrainz artist search for '{Query}' returned {Count} results", 
                query, result.Artists.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search MusicBrainz artists for: {Query}", query);
            throw;
        }
    }

    public async Task<MusicBrainzSearchResult> SearchTracksAsync(string query, int limit = 10)
    {
        var cacheKey = $"mb_track_{query}_{limit}";
        if (_cache.TryGetValue(cacheKey, out MusicBrainzSearchResult? cached) && cached != null)
        {
            return cached;
        }

        await RateLimitAsync();

        try
        {
            var encodedQuery = HttpUtility.UrlEncode(query);
            var response = await _httpClient.GetAsync(
                $"recording?query={encodedQuery}&fmt=json&limit={limit}");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<MusicBrainzRecordingResponse>(json);

            var result = new MusicBrainzSearchResult
            {
                Query = query,
                Type = "track",
                Tracks = searchResult?.Recordings?.Select(r => new MusicBrainzTrack
                {
                    Id = r.Id,
                    Title = r.Title,
                    ArtistCredit = r.ArtistCredit?.Select(ac => new ArtistCredit
                    {
                        Name = ac.Name,
                        Artist = new MusicBrainzArtist
                        {
                            Id = ac.Artist?.Id,
                            Name = ac.Artist?.Name
                        }
                    }).ToList() ?? new List<ArtistCredit>(),
                    Releases = r.Releases?.Select(rel => new MusicBrainzRelease
                    {
                        Id = rel.Id,
                        Title = rel.Title,
                        Date = rel.Date
                    }).ToList() ?? new List<MusicBrainzRelease>(),
                    Length = r.Length,
                    Score = r.Score
                }).ToList() ?? new List<MusicBrainzTrack>(),
                TotalCount = searchResult?.Count ?? 0
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            _logger.LogInformation("MusicBrainz track search for '{Query}' returned {Count} results", 
                query, result.Tracks.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search MusicBrainz tracks for: {Query}", query);
            throw;
        }
    }

    public async Task<MusicBrainzSearchResult> SearchReleasesAsync(string query, int limit = 10)
    {
        var cacheKey = $"mb_release_{query}_{limit}";
        if (_cache.TryGetValue(cacheKey, out MusicBrainzSearchResult? cached) && cached != null)
        {
            return cached;
        }

        await RateLimitAsync();

        try
        {
            var encodedQuery = HttpUtility.UrlEncode(query);
            var response = await _httpClient.GetAsync(
                $"release?query={encodedQuery}&fmt=json&limit={limit}");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<MusicBrainzReleaseSearchResponse>(json);

            var result = new MusicBrainzSearchResult
            {
                Query = query,
                Type = "release",
                Releases = searchResult?.Releases?.Select(r => new MusicBrainzRelease
                {
                    Id = r.Id,
                    Title = r.Title,
                    ArtistCredit = r.ArtistCredit?.Select(ac => new ArtistCredit
                    {
                        Name = ac.Name,
                        Artist = new MusicBrainzArtist
                        {
                            Id = ac.Artist?.Id,
                            Name = ac.Artist?.Name
                        }
                    }).ToList() ?? new List<ArtistCredit>(),
                    Date = r.Date,
                    Country = r.Country,
                    TrackCount = r.TrackCount,
                    Score = r.Score
                }).ToList() ?? new List<MusicBrainzRelease>(),
                TotalCount = searchResult?.Count ?? 0
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            _logger.LogInformation("MusicBrainz release search for '{Query}' returned {Count} results", 
                query, result.Releases.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search MusicBrainz releases for: {Query}", query);
            throw;
        }
    }

    public async Task<MusicBrainzArtist?> GetArtistDetailsAsync(string artistId)
    {
        var cacheKey = $"mb_artist_details_{artistId}";
        if (_cache.TryGetValue(cacheKey, out MusicBrainzArtist? cached) && cached != null)
        {
            return cached;
        }

        await RateLimitAsync();

        try
        {
            var response = await _httpClient.GetAsync(
                $"artist/{artistId}?inc=url-rels+genres+tags&fmt=json");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var artist = JsonSerializer.Deserialize<MusicBrainzArtistDetails>(json);

            if (artist == null) return null;

            var result = new MusicBrainzArtist
            {
                Id = artist.Id,
                Name = artist.Name,
                SortName = artist.SortName,
                Country = artist.Country,
                Type = artist.Type,
                Disambiguation = artist.Disambiguation,
                Genres = artist.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
                Urls = artist.Relations?.Where(r => r.Type == "url")
                    .ToDictionary(r => r.Type, r => r.Url?.Resource) ?? new Dictionary<string, string?>()
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get MusicBrainz artist details for: {ArtistId}", artistId);
            throw;
        }
    }
}

// Service models
public class MusicBrainzSearchResult
{
    public string Query { get; set; } = "";
    public string Type { get; set; } = "";
    public List<MusicBrainzArtist> Artists { get; set; } = new();
    public List<MusicBrainzTrack> Tracks { get; set; } = new();
    public List<MusicBrainzRelease> Releases { get; set; } = new();
    public int TotalCount { get; set; }
}

public class MusicBrainzArtist
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? SortName { get; set; }
    public string? Country { get; set; }
    public string? Type { get; set; }
    public string? Disambiguation { get; set; }
    public int? Score { get; set; }
    public List<string> Genres { get; set; } = new();
    public Dictionary<string, string?> Urls { get; set; } = new();
}

public class MusicBrainzTrack
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public long? Length { get; set; } // in milliseconds
    public int? Score { get; set; }
    public List<ArtistCredit> ArtistCredit { get; set; } = new();
    public List<MusicBrainzRelease> Releases { get; set; } = new();

    public string? FormattedDuration => Length.HasValue 
        ? TimeSpan.FromMilliseconds(Length.Value).ToString(@"mm\:ss") 
        : null;
}

public class MusicBrainzRelease
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Date { get; set; }
    public string? Country { get; set; }
    public int? TrackCount { get; set; }
    public int? Score { get; set; }
    public List<ArtistCredit> ArtistCredit { get; set; } = new();
}

public class ArtistCredit
{
    public string? Name { get; set; }
    public string? JoinPhrase { get; set; }
    public MusicBrainzArtist? Artist { get; set; }
}

// JSON deserialization models
internal class MusicBrainzSearchResponse
{
    public int? Count { get; set; }
    public List<MusicBrainzArtistJson>? Artists { get; set; }
}

internal class MusicBrainzRecordingResponse
{
    public int? Count { get; set; }
    public List<RecordingJson>? Recordings { get; set; }
}

internal class MusicBrainzReleaseSearchResponse
{
    public int? Count { get; set; }
    public List<ReleaseJson>? Releases { get; set; }
}

internal class MusicBrainzArtistJson
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? SortName { get; set; }
    public string? Country { get; set; }
    public string? Type { get; set; }
    public string? Disambiguation { get; set; }
    public int? Score { get; set; }
}

internal class RecordingJson
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public long? Length { get; set; }
    public int? Score { get; set; }
    public List<ArtistCreditJson>? ArtistCredit { get; set; }
    public List<ReleaseJson>? Releases { get; set; }
}

internal class ReleaseJson
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Date { get; set; }
    public string? Country { get; set; }
    public int? TrackCount { get; set; }
    public int? Score { get; set; }
    public List<ArtistCreditJson>? ArtistCredit { get; set; }
}

internal class ArtistCreditJson
{
    public string? Name { get; set; }
    public string? JoinPhrase { get; set; }
    public ArtistJson? Artist { get; set; }
}

internal class ArtistJson
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

internal class MusicBrainzArtistDetails : MusicBrainzArtistJson
{
    public List<GenreJson>? Genres { get; set; }
    public List<RelationJson>? Relations { get; set; }
}

internal class GenreJson
{
    public string? Name { get; set; }
    public int? Count { get; set; }
}

internal class RelationJson
{
    public string? Type { get; set; }
    public UrlJson? Url { get; set; }
}

internal class UrlJson
{
    public string? Resource { get; set; }
    public string? Id { get; set; }
}
