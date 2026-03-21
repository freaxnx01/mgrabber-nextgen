using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MusicGrabber.Modules.Discovery.Application.Ports.Driven;
using MusicGrabber.Modules.Discovery.Domain;

namespace MusicGrabber.Modules.Discovery.Infrastructure.Adapters;

public sealed class MusicBrainzApiClient(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<MusicBrainzApiClient> logger) : IMusicBrainzApi
{
    private static readonly SemaphoreSlim RateLimiter = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<ArtistResult>> SearchArtistsAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        var key = $"mb:artist:{query}:{limit}";
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var json = await RateLimitedGetAsync($"artist/?query={Uri.EscapeDataString(query)}&limit={limit}&fmt=json", ct);
            return ParseArtists(json);
        }) ?? [];
    }

    public async Task<List<TrackResult>> SearchTracksAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        var key = $"mb:track:{query}:{limit}";
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var json = await RateLimitedGetAsync($"recording/?query={Uri.EscapeDataString(query)}&limit={limit}&fmt=json", ct);
            return ParseTracks(json);
        }) ?? [];
    }

    public async Task<List<ReleaseResult>> SearchReleasesAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        var key = $"mb:release:{query}:{limit}";
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var json = await RateLimitedGetAsync($"release/?query={Uri.EscapeDataString(query)}&limit={limit}&fmt=json", ct);
            return ParseReleases(json);
        }) ?? [];
    }

    public async Task<ArtistResult?> GetArtistDetailsAsync(string artistId, CancellationToken ct = default)
    {
        var key = $"mb:artist-detail:{artistId}";
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var json = await RateLimitedGetAsync($"artist/{artistId}?inc=url-rels+genres&fmt=json", ct);
            return ParseSingleArtist(json);
        });
    }

    private async Task<JsonElement> RateLimitedGetAsync(string url, CancellationToken ct)
    {
        await RateLimiter.WaitAsync(ct);
        try
        {
            await Task.Delay(1000, ct); // MusicBrainz rate limit: 1 req/sec
            logger.LogDebug("MusicBrainz request: {Url}", url);
            var response = await httpClient.GetFromJsonAsync<JsonElement>(url, JsonOptions, ct);
            return response;
        }
        finally
        {
            RateLimiter.Release();
        }
    }

    private static List<ArtistResult> ParseArtists(JsonElement json)
    {
        if (!json.TryGetProperty("artists", out var artists))
            return [];

        return artists.EnumerateArray().Select(a => new ArtistResult(
            Id: a.GetProperty("id").GetString() ?? "",
            Name: a.GetProperty("name").GetString() ?? "",
            SortName: a.TryGetProperty("sort-name", out var sn) ? sn.GetString() : null,
            Country: a.TryGetProperty("country", out var c) ? c.GetString() : null,
            Type: a.TryGetProperty("type", out var t) ? t.GetString() : null,
            Disambiguation: a.TryGetProperty("disambiguation", out var d) ? d.GetString() : null,
            Score: a.TryGetProperty("score", out var s) ? s.GetInt32() : 0
        )).ToList();
    }

    private static List<TrackResult> ParseTracks(JsonElement json)
    {
        if (!json.TryGetProperty("recordings", out var recordings))
            return [];

        return recordings.EnumerateArray().Select(r =>
        {
            var length = r.TryGetProperty("length", out var l) ? l.GetInt64() : 0;
            var duration = length > 0 ? TimeSpan.FromMilliseconds(length).ToString(@"m\:ss") : null;
            var artistCredit = r.TryGetProperty("artist-credit", out var ac)
                ? string.Join(", ", ac.EnumerateArray().Select(a => a.GetProperty("name").GetString()))
                : null;

            return new TrackResult(
                Id: r.GetProperty("id").GetString() ?? "",
                Title: r.GetProperty("title").GetString() ?? "",
                FormattedDuration: duration,
                Score: r.TryGetProperty("score", out var s) ? s.GetInt32() : 0,
                ArtistCredit: artistCredit
            );
        }).ToList();
    }

    private static List<ReleaseResult> ParseReleases(JsonElement json)
    {
        if (!json.TryGetProperty("releases", out var releases))
            return [];

        return releases.EnumerateArray().Select(r =>
        {
            var artistCredit = r.TryGetProperty("artist-credit", out var ac)
                ? string.Join(", ", ac.EnumerateArray().Select(a => a.GetProperty("name").GetString()))
                : null;

            return new ReleaseResult(
                Id: r.GetProperty("id").GetString() ?? "",
                Title: r.GetProperty("title").GetString() ?? "",
                Date: r.TryGetProperty("date", out var d) ? d.GetString() : null,
                Country: r.TryGetProperty("country", out var c) ? c.GetString() : null,
                Score: r.TryGetProperty("score", out var s) ? s.GetInt32() : 0,
                ArtistCredit: artistCredit
            );
        }).ToList();
    }

    private static ArtistResult? ParseSingleArtist(JsonElement json)
    {
        if (json.ValueKind == JsonValueKind.Undefined)
            return null;

        return new ArtistResult(
            Id: json.GetProperty("id").GetString() ?? "",
            Name: json.GetProperty("name").GetString() ?? "",
            SortName: json.TryGetProperty("sort-name", out var sn) ? sn.GetString() : null,
            Country: json.TryGetProperty("country", out var c) ? c.GetString() : null,
            Type: json.TryGetProperty("type", out var t) ? t.GetString() : null,
            Disambiguation: json.TryGetProperty("disambiguation", out var d) ? d.GetString() : null,
            Score: 100
        );
    }
}
