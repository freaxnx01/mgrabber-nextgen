using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.Application.Ports.Driving;

public interface IUserSettingsService
{
    Task<UserSettings> GetOrCreateAsync(string userId, CancellationToken ct = default);
    Task<UserSettings> UpdateAsync(string userId, string defaultFormat, bool enableNormalization, int normalizationLufs, bool emailNotifications, CancellationToken ct = default);
}
