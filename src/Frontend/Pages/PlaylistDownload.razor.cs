using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using MusicGrabber.Frontend.Services;
using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Pages;

[Authorize]
public partial class PlaylistDownload : IAsyncDisposable
{
    [Inject] private IDownloadFrontendService DownloadService { get; set; } = null!;
    [Inject] private IQuotaFrontendService QuotaService { get; set; } = null!;
    [Inject] private IIdentityFrontendService IdentityService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ILogger<PlaylistDownload> Logger { get; set; } = default!;

    private string _playlistUrl = string.Empty;
    private string _selectedFormat = "Mp3";
    private bool _normalizeAudio;
    private bool _isLoading;
    private bool _isDownloading;
    private string _userId = string.Empty;
    private List<YouTubeSearchResultDto>? _playlistVideos;
    private HashSet<YouTubeSearchResultDto> _selectedVideos = [];
    private List<DownloadJob>? _activeJobs;
    private QuotaInfoDto? _quota;
    private HubConnection? _hubConnection;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        await LoadUserSettingsAsync();
        await LoadQuotaAsync();
        await SetupSignalRAsync();
    }

    private async Task LoadUserSettingsAsync()
    {
        try
        {
            var settings = await IdentityService.GetOrCreateSettingsAsync(_userId);
            _selectedFormat = settings.DefaultFormat;
            _normalizeAudio = settings.EnableNormalization;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load user settings for user {UserId}, using defaults", _userId);
        }
    }

    private async Task LoadQuotaAsync()
    {
        try
        {
            _quota = await QuotaService.GetQuotaAsync(_userId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load quota for user {UserId}", _userId);
        }
    }

    private async Task LoadPlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(_playlistUrl)) return;

        _isLoading = true;
        _playlistVideos = null;
        _selectedVideos = [];

        try
        {
            // Search using the playlist URL as query; the backend will resolve it
            _playlistVideos = await DownloadService.SearchYouTubeAsync(_playlistUrl);
            _selectedVideos = new HashSet<YouTubeSearchResultDto>(_playlistVideos);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load playlist from URL {PlaylistUrl}", _playlistUrl);
            Snackbar.Add($"Failed to load playlist: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task DownloadSelectedAsync()
    {
        if (_selectedVideos.Count == 0) return;

        _isDownloading = true;
        try
        {
            foreach (var video in _selectedVideos)
            {
                var request = new StartDownloadRequest(
                    $"https://www.youtube.com/watch?v={video.VideoId}",
                    _userId, _selectedFormat, video.Title, video.Author,
                    _normalizeAudio);

                await DownloadService.StartDownloadAsync(request);
            }

            Snackbar.Add($"Started {_selectedVideos.Count} downloads!", Severity.Info);
            await LoadActiveJobsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start batch downloads for {Count} videos", _selectedVideos.Count);
            Snackbar.Add($"Failed to start downloads: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isDownloading = false;
        }
    }

    private async Task DownloadSingleAsync(YouTubeSearchResultDto video)
    {
        try
        {
            var request = new StartDownloadRequest(
                $"https://www.youtube.com/watch?v={video.VideoId}",
                _userId, _selectedFormat, video.Title, video.Author,
                _normalizeAudio);

            await DownloadService.StartDownloadAsync(request);
            Snackbar.Add("Download started!", Severity.Info);
            await LoadActiveJobsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start download for video {VideoId}", video.VideoId);
            Snackbar.Add($"Failed to start download: {ex.Message}", Severity.Error);
        }
    }

    private async Task LoadActiveJobsAsync()
    {
        try
        {
            var jobs = await DownloadService.GetUserJobsAsync(_userId);
            _activeJobs = jobs
                .Where(j => j.Status is DownloadStatus.Pending or DownloadStatus.Downloading or DownloadStatus.Normalizing)
                .OrderByDescending(j => j.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load active jobs for user {UserId}", _userId);
            _activeJobs = [];
        }
    }

    private async Task SetupSignalRAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/hubs/download"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<Guid, int, string>("ReceiveProgress", async (jobId, progress, _) =>
        {
            var job = _activeJobs?.FirstOrDefault(j => j.Id == jobId);
            if (job is not null)
            {
                job.UpdateProgress(progress);
                await InvokeAsync(StateHasChanged);
            }
        });

        _hubConnection.On<Guid, FileInfoDto>("ReceiveCompleted", async (_, _) =>
        {
            await LoadActiveJobsAsync();
            await LoadQuotaAsync();
            Snackbar.Add("Download completed!", Severity.Success);
            await InvokeAsync(StateHasChanged);
        });

        _hubConnection.On<Guid, string>("ReceiveFailed", async (_, error) =>
        {
            await LoadActiveJobsAsync();
            Snackbar.Add($"Download failed: {error}", Severity.Error);
            await InvokeAsync(StateHasChanged);
        });

        try
        {
            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "SignalR connection failed, real-time updates will be unavailable");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }
}
