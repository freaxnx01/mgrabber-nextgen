using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MusicGrabber.Frontend.Services;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Pages.Admin;

[Authorize(Roles = "Admin")]
public partial class Statistics
{
    [Inject] private IDownloadFrontendService DownloadService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private ILogger<Statistics> Logger { get; set; } = default!;

    private bool _isLoading = true;
    private bool _showUserDialog;
    private GlobalStatsDto? _stats;
    private UserStatsDto? _selectedUser;
    private int _maxDayDownloads;

    private string SuccessRate
    {
        get
        {
            if (_stats is null || _stats.TotalDownloads == 0) return "N/A";
            var completed = _stats.StatusCounts.GetValueOrDefault("Completed", 0);
            return $"{(double)completed / _stats.TotalDownloads * 100:F1}%";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadStatsAsync();
    }

    private async Task LoadStatsAsync()
    {
        _isLoading = true;
        try
        {
            _stats = await DownloadService.GetGlobalStatsAsync();
            _maxDayDownloads = _stats.DownloadsPerDay.Count > 0
                ? _stats.DownloadsPerDay.Values.Max()
                : 1;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load global statistics");
            Snackbar.Add($"Failed to load statistics: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private double GetDayPercentage(int count) =>
        _maxDayDownloads > 0 ? (double)count / _maxDayDownloads * 100 : 0;

    private void ShowUserDetails(UserStatsDto user)
    {
        _selectedUser = user;
        _showUserDialog = true;
    }

    private void CloseUserDialog()
    {
        _showUserDialog = false;
        _selectedUser = null;
    }

    private static Color GetStatusColor(string status) => status switch
    {
        "Completed" => Color.Success,
        "Failed" => Color.Error,
        "Pending" => Color.Warning,
        "Downloading" => Color.Info,
        "Normalizing" => Color.Info,
        _ => Color.Default
    };

    private static string GetUserSuccessRate(UserStatsDto user)
    {
        if (user.TotalDownloads == 0) return "N/A";
        return $"{(double)user.CompletedDownloads / user.TotalDownloads * 100:F1}%";
    }

    private static string FormatBytes(long bytes)
    {
        const double mb = 1024 * 1024;
        const double gb = 1024 * 1024 * 1024;
        return bytes >= gb ? $"{bytes / gb:F1} GB" : $"{bytes / mb:F0} MB";
    }
}
