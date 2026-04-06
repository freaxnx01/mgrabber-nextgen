using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MusicGrabber.Frontend.Services;
using MusicGrabber.Modules.Discovery.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Pages;

[Authorize]
public partial class MusicBrainzSearch
{
    private const string SearchTypeArtist = "Artist";
    private const string SearchTypeTrack = "Track";
    private const string SearchTypeAlbum = "Album";

    [Inject] private IDiscoveryFrontendService DiscoveryService { get; set; } = null!;
    [Inject] private IDownloadFrontendService DownloadService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private ILogger<MusicBrainzSearch> Logger { get; set; } = default!;

    private string _searchQuery = string.Empty;
    private string _searchType = SearchTypeArtist;
    private bool _isSearching;
    private bool _isSearchingYouTube;

    private List<ArtistResult>? _artists;
    private List<TrackResult>? _tracks;
    private List<ReleaseResult>? _releases;

    private List<YouTubeSearchResultDto>? _youTubeResults;
    private string _youTubeQuery = string.Empty;
    private string _userId = string.Empty;

    private bool HasResults => (_artists is { Count: > 0 }) || (_tracks is { Count: > 0 }) || (_releases is { Count: > 0 });
    private int ResultCount => (_artists?.Count ?? 0) + (_tracks?.Count ?? 0) + (_releases?.Count ?? 0);

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery)) return;

        _isSearching = true;
        _artists = null;
        _tracks = null;
        _releases = null;

        try
        {
            switch (_searchType)
            {
                case SearchTypeArtist:
                    _artists = await DiscoveryService.SearchArtistsAsync(_searchQuery);
                    break;
                case SearchTypeTrack:
                    _tracks = await DiscoveryService.SearchTracksAsync(_searchQuery);
                    break;
                case SearchTypeAlbum:
                    _releases = await DiscoveryService.SearchReleasesAsync(_searchQuery);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MusicBrainz search failed for query {Query} with type {SearchType}", _searchQuery, _searchType);
            Snackbar.Add($"Search failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSearching = false;
        }
    }

    private Task SearchYouTubeForTrackAsync(TrackResult track)
        => SearchYouTubeForAsync($"{track.ArtistCredit} {track.Title}");

    private Task SearchYouTubeForReleaseAsync(ReleaseResult release)
        => SearchYouTubeForAsync($"{release.ArtistCredit} {release.Title}");

    private async Task SearchYouTubeForAsync(string query)
    {
        _youTubeQuery = query;
        _isSearchingYouTube = true;
        _youTubeResults = null;
        StateHasChanged();

        try
        {
            _youTubeResults = await DownloadService.SearchYouTubeAsync(query);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "YouTube search failed for query {Query}", query);
            Snackbar.Add($"YouTube search failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSearchingYouTube = false;
        }
    }

    private async Task DownloadDefaultAsync(YouTubeSearchResultDto result)
    {
        try
        {
            var request = new StartDownloadRequest(
                $"https://www.youtube.com/watch?v={result.VideoId}",
                _userId, "Mp3", result.Title, result.Author);

            await DownloadService.StartDownloadAsync(request);
            Snackbar.Add("Download started!", Severity.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start download for video {VideoId}", result.VideoId);
            Snackbar.Add($"Failed to start download: {ex.Message}", Severity.Error);
        }
    }

    private async Task DownloadWithFormatAsync((YouTubeSearchResultDto Result, string Format) args)
    {
        try
        {
            var request = new StartDownloadRequest(
                $"https://www.youtube.com/watch?v={args.Result.VideoId}",
                _userId, args.Format, args.Result.Title, args.Result.Author);

            await DownloadService.StartDownloadAsync(request);
            Snackbar.Add("Download started!", Severity.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start download for video {VideoId} with format {Format}", args.Result.VideoId, args.Format);
            Snackbar.Add($"Failed to start download: {ex.Message}", Severity.Error);
        }
    }

    private void CloseYouTubeResults()
    {
        _youTubeResults = null;
        _youTubeQuery = string.Empty;
    }

    private void ClearResults()
    {
        _searchQuery = string.Empty;
        _artists = null;
        _tracks = null;
        _releases = null;
        _youTubeResults = null;
        _youTubeQuery = string.Empty;
    }
}
