using MusicGrabber.Modules.Identity.Application.Ports.Driving;
using MusicGrabber.Modules.Identity.Application.UseCases.UpdateSettings;
using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.Infrastructure.Adapters;

internal sealed class UserSettingsServiceAdapter : IUserSettingsService
{
    private readonly UpdateSettingsHandler _handler;

    public UserSettingsServiceAdapter(UpdateSettingsHandler handler)
    {
        _handler = handler;
    }

    public Task<UserSettings> GetOrCreateAsync(string userId, CancellationToken ct = default) =>
        _handler.GetOrCreateAsync(userId, ct);

    public Task<UserSettings> UpdateAsync(string userId, string defaultFormat, bool enableNormalization, int normalizationLufs, bool emailNotifications, CancellationToken ct = default) =>
        _handler.UpdateAsync(userId, defaultFormat, enableNormalization, normalizationLufs, emailNotifications, ct);
}
