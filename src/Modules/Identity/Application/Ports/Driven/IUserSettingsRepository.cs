using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.Application.Ports.Driven;

public interface IUserSettingsRepository
{
    Task<UserSettings?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task UpsertAsync(UserSettings settings, CancellationToken ct = default);
}
