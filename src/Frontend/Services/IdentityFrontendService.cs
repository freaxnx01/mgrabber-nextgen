using MusicGrabber.Modules.Identity.Application.Ports.Driving;
using MusicGrabber.Modules.Identity.Domain;
using MusicGrabber.Shared.Contracts;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Services;

public sealed class IdentityFrontendService(
    IUserFacade userFacade,
    IUserSettingsService userSettingsService,
    IWhitelistService whitelistService) : IIdentityFrontendService
{
    public Task<UserProfileDto?> GetProfileAsync(string userId, CancellationToken ct)
        => userFacade.GetProfileAsync(userId, ct);

    public Task<UserSettings> GetOrCreateSettingsAsync(string userId, CancellationToken ct)
        => userSettingsService.GetOrCreateAsync(userId, ct);

    public Task<UserSettings> UpdateSettingsAsync(string userId, string defaultFormat, bool enableNormalization, int normalizationLufs, bool emailNotifications, CancellationToken ct)
        => userSettingsService.UpdateAsync(userId, defaultFormat, enableNormalization, normalizationLufs, emailNotifications, ct);

    public Task<IReadOnlyList<WhitelistEntry>> GetWhitelistAsync(CancellationToken ct)
        => whitelistService.GetAllAsync(ct);

    public Task<WhitelistEntry> AddWhitelistEntryAsync(string userId, string role, string addedBy, CancellationToken ct)
        => whitelistService.AddAsync(userId, role, addedBy, ct);

    public Task<WhitelistEntry> ToggleWhitelistEntryAsync(Guid id, CancellationToken ct)
        => whitelistService.ToggleAsync(id, ct);

    public Task RemoveWhitelistEntryAsync(Guid id, CancellationToken ct)
        => whitelistService.RemoveAsync(id, ct);
}
