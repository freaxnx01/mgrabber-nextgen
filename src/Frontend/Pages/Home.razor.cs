using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using MusicGrabber.Frontend.Services;
using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Pages;

[Authorize]
public partial class Home : IAsyncDisposable
{
    [Inject] private IDownloadFrontendService DownloadService { get; set; } = null!;
    [Inject] private IIdentityFrontendService IdentityService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ILogger<Home> Logger { get; set; } = default!;

    private string _searchQuery = string.Empty;
    private bool _isSearching;
    private bool _isLoadingFiles = true;
    private List<YouTubeSearchResultDto>? _searchResults;
    private List<DownloadJob>? _activeJobs;
    private List<FileInfoDto>? _userFiles;
    private string _userId = string.Empty;
    private string _defaultFormat = "Mp3";
    private HubConnection? _hubConnection;

    protected override async Task OnInitializedAsync()
    {
        _userId = await GetUserIdAsync();
        if (string.IsNullOrEmpty(_userId)) return;

        await LoadUserSettingsAsync();
        await LoadActiveJobsAsync();
        await LoadUserFilesAsync();
        await SetupSignalRAsync();
    }

    private async Task<string> GetUserIdAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    private async Task LoadUserSettingsAsync()
    {
        try
        {
            var settings = await IdentityService.GetOrCreateSettingsAsync(_userId);
            _defaultFormat = settings.DefaultFormat;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load user settings for user {UserId}, using defaults", _userId);
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

    private async Task LoadUserFilesAsync()
    {
        try
        {
            var files = await DownloadService.GetUserFilesAsync(_userId);
            _userFiles = files.Select(f => new FileInfoDto(
                f.Id, f.Title ?? "Unknown", f.Author ?? "Unknown",
                f.Format.ToString(), f.CorrectedFilename ?? "Unknown",
                f.FileSizeBytes, f.CompletedAt ?? DateTime.UtcNow))
                .OrderByDescending(f => f.CompletedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load user files for user {UserId}", _userId);
            _userFiles = [];
        }
        finally
        {
            _isLoadingFiles = false;
        }
    }

    private async Task SetupSignalRAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/hubs/download"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<Guid, int, string>("ReceiveProgress", async (jobId, progress, status) =>
        {
            var job = _activeJobs?.FirstOrDefault(j => j.Id == jobId);
            if (job is not null)
            {
                job.UpdateProgress(progress);
                await InvokeAsync(StateHasChanged);
            }
        });

        _hubConnection.On<Guid, FileInfoDto>("ReceiveCompleted", async (jobId, _) =>
        {
            await LoadActiveJobsAsync();
            await LoadUserFilesAsync();
            Snackbar.Add("Download completed!", Severity.Success);
            await InvokeAsync(StateHasChanged);
        });

        _hubConnection.On<Guid, string>("ReceiveFailed", async (jobId, error) =>
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

    private async Task HandleSearchKeyUp(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
            await SearchAsync();
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery)) return;

        _isSearching = true;
        _searchResults = null;

        try
        {
            _searchResults = await DownloadService.SearchYouTubeAsync(_searchQuery);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "YouTube search failed for query {Query}", _searchQuery);
            Snackbar.Add($"Search failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSearching = false;
        }
    }

    private Task DownloadDefaultAsync(YouTubeSearchResultDto result)
        => StartDownloadAsync(result, _defaultFormat);

    private Task DownloadWithFormatAsync((YouTubeSearchResultDto Result, string Format) args)
        => StartDownloadAsync(args.Result, args.Format);

    private async Task StartDownloadAsync(YouTubeSearchResultDto result, string format)
    {
        try
        {
            var request = new StartDownloadRequest(
                $"https://www.youtube.com/watch?v={result.VideoId}",
                _userId, format, result.Title, result.Author);

            await DownloadService.StartDownloadAsync(request);
            Snackbar.Add("Download started!", Severity.Info);
            await LoadActiveJobsAsync();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start download for video {VideoId}", result.VideoId);
            Snackbar.Add($"Failed to start download: {ex.Message}", Severity.Error);
        }
    }

    private async Task ConfirmDeleteAsync(FileInfoDto file)
    {
        var result = await DialogService.ShowMessageBoxAsync(
            "Delete File",
            $"Are you sure you want to delete '{file.Title}'?",
            yesText: "Delete",
            cancelText: "Cancel");

        if (result == true)
        {
            try
            {
                await DownloadService.DeleteFileAsync(file.JobId, _userId);
                Snackbar.Add("File deleted.", Severity.Success);
                await LoadUserFilesAsync();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete file for job {JobId}", file.JobId);
                Snackbar.Add($"Failed to delete: {ex.Message}", Severity.Error);
            }
        }
    }

    private static string GetDownloadUrl(Guid jobId) => $"/api/v1/files/{jobId}/download";

    private static string FormatBytes(long bytes)
    {
        const double mb = 1024 * 1024;
        const double gb = 1024 * 1024 * 1024;
        return bytes >= gb ? $"{bytes / gb:F1} GB" : $"{bytes / mb:F1} MB";
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
