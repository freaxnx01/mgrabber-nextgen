using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MusicGrabber.Frontend.Services;
using MusicGrabber.Modules.Radio.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Pages;

[Authorize]
public partial class Radio : IDisposable
{
    [Inject] private IRadioFrontendService RadioService { get; set; } = null!;
    [Inject] private IDownloadFrontendService DownloadService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private string _userId = string.Empty;
    private bool _isLoadingStations = true;
    private bool _isSearchingYouTube;
    private bool _showYouTubeDialog;
    private string _youTubeQuery = string.Empty;

    private List<RadioStation>? _stations;
    private string? _selectedStationId;
    private RadioSong? _nowPlaying;
    private List<RadioSong>? _playlist;
    private List<YouTubeSearchResultDto>? _youTubeResults;
    private Timer? _refreshTimer;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        await LoadStationsAsync();
    }

    private async Task LoadStationsAsync()
    {
        _isLoadingStations = true;
        try
        {
            _stations = await RadioService.GetStationsAsync();
            if (_stations is { Count: > 0 })
            {
                _selectedStationId = _stations[0].Id;
                await LoadStationDataAsync();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load stations: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoadingStations = false;
        }
    }

    private async Task OnStationChangedAsync(string? stationId)
    {
        _selectedStationId = stationId;
        _nowPlaying = null;
        _playlist = null;
        StopTimer();

        if (stationId is not null)
        {
            await LoadStationDataAsync();
        }
    }

    private async Task LoadStationDataAsync()
    {
        if (_selectedStationId is null) return;

        try
        {
            _nowPlaying = await RadioService.GetNowPlayingAsync(_selectedStationId);
            _playlist = await RadioService.GetPlaylistAsync(_selectedStationId, 20);
            StartTimer();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load station data: {ex.Message}", Severity.Error);
        }
    }

    private void StartTimer()
    {
        StopTimer();
        _refreshTimer = new Timer(async _ =>
        {
            await InvokeAsync(async () =>
            {
                await LoadStationDataAsync();
                StateHasChanged();
            });
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private void StopTimer()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }

    private async Task ExtractAudioAsync()
    {
        if (_nowPlaying is null || _selectedStationId is null) return;
        await ExtractSongAsync(_nowPlaying);
    }

    private async Task ExtractSongAsync(RadioSong song)
    {
        try
        {
            // Search YouTube for best match and start download
            var results = await DownloadService.SearchYouTubeAsync($"{song.Artist} {song.Title}", 1);
            if (results is { Count: > 0 })
            {
                var best = results[0];
                var request = new StartDownloadRequest(
                    $"https://www.youtube.com/watch?v={best.VideoId}",
                    _userId, "Mp3", song.Title, song.Artist);
                await DownloadService.StartDownloadAsync(request);
                Snackbar.Add($"Extracting: {song.Artist} - {song.Title}", Severity.Info);
            }
            else
            {
                // No match found, open YouTube dialog for manual selection
                await SearchYouTubeAsync(song);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Extract failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task SearchYouTubeAsync(RadioSong song)
    {
        _youTubeQuery = $"{song.Artist} {song.Title}";
        _isSearchingYouTube = true;
        _showYouTubeDialog = true;
        _youTubeResults = null;
        StateHasChanged();

        try
        {
            _youTubeResults = await DownloadService.SearchYouTubeAsync(_youTubeQuery);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"YouTube search failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSearchingYouTube = false;
        }
    }

    private async Task DownloadFromDialogAsync(YouTubeSearchResultDto result)
    {
        try
        {
            var request = new StartDownloadRequest(
                $"https://www.youtube.com/watch?v={result.VideoId}",
                _userId, "Mp3", result.Title, result.Author);
            await DownloadService.StartDownloadAsync(request);
            Snackbar.Add("Download started!", Severity.Info);
            _showYouTubeDialog = false;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Download failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task DownloadWithFormatFromDialogAsync((YouTubeSearchResultDto Result, string Format) args)
    {
        try
        {
            var request = new StartDownloadRequest(
                $"https://www.youtube.com/watch?v={args.Result.VideoId}",
                _userId, args.Format, args.Result.Title, args.Result.Author);
            await DownloadService.StartDownloadAsync(request);
            Snackbar.Add("Download started!", Severity.Info);
            _showYouTubeDialog = false;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Download failed: {ex.Message}", Severity.Error);
        }
    }

    private void CloseYouTubeDialog()
    {
        _showYouTubeDialog = false;
        _youTubeResults = null;
    }

    private static string FormatDuration(DateTimeOffset start, DateTimeOffset end)
    {
        var duration = end - start;
        return duration.TotalMinutes >= 1 ? $"{duration.Minutes}:{duration.Seconds:D2}" : $"0:{duration.Seconds:D2}";
    }

    public void Dispose()
    {
        StopTimer();
    }
}
