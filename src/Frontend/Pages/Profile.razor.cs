using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MusicGrabber.Frontend.Services;
using MusicGrabber.Modules.Identity.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Pages;

[Authorize]
public partial class Profile
{
    [Inject] private IIdentityFrontendService IdentityService { get; set; } = null!;
    [Inject] private IDownloadFrontendService DownloadService { get; set; } = null!;
    [Inject] private IQuotaFrontendService QuotaService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private ILogger<Profile> Logger { get; set; } = default!;

    private string _userId = string.Empty;
    private bool _isSaving;
    private UserProfileDto? _profile;
    private UserSettings? _settings;
    private QuotaInfoDto? _quota;
    private UserStatsDto? _stats;

    private int ActiveDownloads => (_stats?.TotalDownloads ?? 0) - (_stats?.CompletedDownloads ?? 0) - (_stats?.FailedDownloads ?? 0);

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        await Task.WhenAll(
            LoadProfileAsync(),
            LoadSettingsAsync(),
            LoadQuotaAsync(),
            LoadStatsAsync());
    }

    private async Task LoadProfileAsync()
    {
        try { _profile = await IdentityService.GetProfileAsync(_userId); }
        catch (Exception ex) { Logger.LogWarning(ex, "Failed to load profile for user {UserId}", _userId); }
    }

    private async Task LoadSettingsAsync()
    {
        try { _settings = await IdentityService.GetOrCreateSettingsAsync(_userId); }
        catch (Exception ex) { Logger.LogWarning(ex, "Failed to load settings for user {UserId}", _userId); }
    }

    private async Task LoadQuotaAsync()
    {
        try { _quota = await QuotaService.GetQuotaAsync(_userId); }
        catch (Exception ex) { Logger.LogWarning(ex, "Failed to load quota for user {UserId}", _userId); }
    }

    private async Task LoadStatsAsync()
    {
        try { _stats = await DownloadService.GetUserStatsAsync(_userId); }
        catch (Exception ex) { Logger.LogWarning(ex, "Failed to load stats for user {UserId}", _userId); }
    }

    private async Task SaveSettingsAsync()
    {
        if (_settings is null) return;

        _isSaving = true;
        try
        {
            _settings = await IdentityService.UpdateSettingsAsync(
                _userId,
                _settings.DefaultFormat,
                _settings.EnableNormalization,
                _settings.NormalizationLufs,
                _settings.EmailNotifications);

            Snackbar.Add("Settings saved!", Severity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save settings for user {UserId}", _userId);
            Snackbar.Add($"Failed to save settings: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant()
        };
    }

    private static string FormatBytes(long bytes)
    {
        const double mb = 1024 * 1024;
        const double gb = 1024 * 1024 * 1024;
        return bytes >= gb ? $"{bytes / gb:F1} GB" : $"{bytes / mb:F0} MB";
    }
}
