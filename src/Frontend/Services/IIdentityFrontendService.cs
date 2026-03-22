using MusicGrabber.Modules.Identity.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Services;

public interface IIdentityFrontendService
{
    Task<UserProfileDto?> GetProfileAsync(string userId, CancellationToken ct = default);
    Task<UserSettings> GetOrCreateSettingsAsync(string userId, CancellationToken ct = default);
    Task<UserSettings> UpdateSettingsAsync(string userId, string defaultFormat, bool enableNormalization, int normalizationLufs, bool emailNotifications, CancellationToken ct = default);
    Task<IReadOnlyList<WhitelistEntry>> GetWhitelistAsync(CancellationToken ct = default);
    Task<WhitelistEntry> AddWhitelistEntryAsync(string userId, string role, string addedBy, CancellationToken ct = default);
    Task<WhitelistEntry> ToggleWhitelistEntryAsync(Guid id, CancellationToken ct = default);
    Task RemoveWhitelistEntryAsync(Guid id, CancellationToken ct = default);
}
