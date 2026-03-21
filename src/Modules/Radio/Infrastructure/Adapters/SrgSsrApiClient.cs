using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MusicGrabber.Modules.Radio.Application.Ports.Driven;
using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Modules.Radio.Infrastructure.Adapters;

public sealed class SrgSsrApiClient(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<SrgSsrApiClient> logger) : ISrgSsrApi
{
    private static readonly TimeSpan StationCacheTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan NowPlayingCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly List<RadioStation> HardcodedStations =
    [
        new RadioStation("srf-1", "SRF 1"),
        new RadioStation("srf-3", "SRF 3"),
        new RadioStation("srf-virus", "SRF Virus")
    ];

    private static readonly Dictionary<string, string> StationChannelIds = new()
    {
        ["srf-1"] = "69e8ac16-4327-4af4-b873-fd5cd6e895a7",
        ["srf-3"] = "c8537421-c9c5-4c4f-aa43-3e6f0366013d",
        ["srf-virus"] = "66815fe2-9008-4853-80a5-f9caaffdf3a9"
    };

    public Task<List<RadioStation>> GetStationsAsync(CancellationToken ct = default)
    {
        return cache.GetOrCreateAsync("radio:stations", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = StationCacheTtl;
            logger.LogDebug("Returning hardcoded SRG SSR station list");
            return Task.FromResult(HardcodedStations);
        })!;
    }

    public async Task<RadioSong?> GetNowPlayingAsync(string stationId, CancellationToken ct = default)
    {
        var key = $"radio:now-playing:{stationId}";
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = NowPlayingCacheTtl;
            var songs = await FetchPlaylistAsync(stationId, 1, ct);
            return songs.FirstOrDefault();
        });
    }

    public async Task<List<RadioSong>> GetPlaylistAsync(string stationId, int limit = 10, CancellationToken ct = default)
    {
        var key = $"radio:playlist:{stationId}:{limit}";
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = NowPlayingCacheTtl;
            return await FetchPlaylistAsync(stationId, limit, ct);
        }) ?? [];
    }

    private async Task<List<RadioSong>> FetchPlaylistAsync(string stationId, int limit, CancellationToken ct)
    {
        if (!StationChannelIds.TryGetValue(stationId, out var channelId))
        {
            logger.LogWarning("Unknown station ID: {StationId}", stationId);
            return [];
        }

        try
        {
            var url = $"srf/songList/radio/byChannel/{channelId}?pageSize={limit}";
            logger.LogDebug("Fetching SRG SSR playlist for station {StationId}", stationId);
            var json = await httpClient.GetFromJsonAsync<JsonElement>(url, JsonOptions, ct);
            return ParseSongs(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to fetch SRG SSR playlist for station {StationId}", stationId);
            return [];
        }
    }

    private static List<RadioSong> ParseSongs(JsonElement json)
    {
        if (!json.TryGetProperty("songList", out var songList))
            return [];

        var results = new List<RadioSong>();
        foreach (var item in songList.EnumerateArray())
        {
            var artist = item.TryGetProperty("artistDisplayName", out var a) ? a.GetString() ?? "" : "";
            var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";

            DateTimeOffset startTime = default;
            DateTimeOffset endTime = default;

            if (item.TryGetProperty("playbackStarted", out var ps) && ps.GetString() is { } psStr)
                DateTimeOffset.TryParse(psStr, out startTime);

            if (item.TryGetProperty("playbackEnded", out var pe) && pe.GetString() is { } peStr)
                DateTimeOffset.TryParse(peStr, out endTime);

            results.Add(new RadioSong(artist, title, startTime, endTime));
        }

        return results;
    }
}
