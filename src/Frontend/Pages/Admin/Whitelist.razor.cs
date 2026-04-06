using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MusicGrabber.Frontend.Services;
using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Frontend.Pages.Admin;

[Authorize(Roles = "Admin")]
public partial class Whitelist
{
    [Inject] private IIdentityFrontendService IdentityService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private ILogger<Whitelist> Logger { get; set; } = default!;

    private string _userId = string.Empty;
    private string _searchFilter = string.Empty;
    private bool _isLoading = true;

    private bool _showAddDialog;
    private bool _showRemoveDialog;
    private string _newEmail = string.Empty;
    private string _newRole = "User";
    private bool _sendWelcomeEmail = true;
    private MudForm? _addForm;

    private WhitelistEntryViewModel? _entryToRemove;
    private List<WhitelistEntryViewModel> _entries = [];

    private IEnumerable<WhitelistEntryViewModel> FilteredEntries =>
        string.IsNullOrWhiteSpace(_searchFilter)
            ? _entries
            : _entries.Where(e => e.UserId.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await LoadWhitelistAsync();
    }

    private async Task LoadWhitelistAsync()
    {
        _isLoading = true;
        try
        {
            var entries = await IdentityService.GetWhitelistAsync();
            _entries = entries.Select(e => new WhitelistEntryViewModel(
                e.Id, e.UserId, e.Role, e.AddedBy, e.IsActive, e.AddedAt)).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load whitelist");
            Snackbar.Add($"Failed to load whitelist: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void OpenAddDialog()
    {
        _newEmail = string.Empty;
        _newRole = "User";
        _sendWelcomeEmail = true;
        _showAddDialog = true;
    }

    private void CloseAddDialog() => _showAddDialog = false;

    private async Task AddUserAsync()
    {
        if (string.IsNullOrWhiteSpace(_newEmail)) return;

        try
        {
            await IdentityService.AddWhitelistEntryAsync(_newEmail, _newRole, _userId);
            Snackbar.Add($"Added {_newEmail} to whitelist.", Severity.Success);
            _showAddDialog = false;
            await LoadWhitelistAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to add user {Email} to whitelist", _newEmail);
            Snackbar.Add($"Failed to add user: {ex.Message}", Severity.Error);
        }
    }

    private async Task ToggleStatusAsync(WhitelistEntryViewModel entry)
    {
        try
        {
            await IdentityService.ToggleWhitelistEntryAsync(entry.Id);
            Snackbar.Add($"{entry.UserId} status toggled.", Severity.Info);
            await LoadWhitelistAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to toggle whitelist status for entry {EntryId}", entry.Id);
            Snackbar.Add($"Failed to toggle status: {ex.Message}", Severity.Error);
        }
    }

    private void ConfirmRemoveAsync(WhitelistEntryViewModel entry)
    {
        _entryToRemove = entry;
        _showRemoveDialog = true;
    }

    private void CloseRemoveDialog() => _showRemoveDialog = false;

    private async Task RemoveUserAsync()
    {
        if (_entryToRemove is null) return;

        try
        {
            await IdentityService.RemoveWhitelistEntryAsync(_entryToRemove.Id);
            Snackbar.Add($"Removed {_entryToRemove.UserId}.", Severity.Success);
            _showRemoveDialog = false;
            _entryToRemove = null;
            await LoadWhitelistAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove whitelist entry {EntryId}", _entryToRemove?.Id);
            Snackbar.Add($"Failed to remove user: {ex.Message}", Severity.Error);
        }
    }

    public sealed record WhitelistEntryViewModel(
        Guid Id, string UserId, string Role, string AddedBy, bool IsActive, DateTime AddedAt);
}
