using MusicGrabber.Modules.Identity.Application.Ports.Driven;
using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.Application.UseCases.UpdateSettings;

public sealed class UpdateSettingsHandler
{
    private readonly IUserSettingsRepository _repo;

    public UpdateSettingsHandler(IUserSettingsRepository repo)
    {
        _repo = repo;
    }

    public async Task<UserSettings> GetOrCreateAsync(string userId, CancellationToken ct = default)
    {
        var existing = await _repo.GetByUserIdAsync(userId, ct);
        if (existing is not null)
        {
            return existing;
        }

        var settings = new UserSettings { UserId = userId };
        await _repo.UpsertAsync(settings, ct);

        return settings;
    }

    public async Task<UserSettings> UpdateAsync(
        string userId,
        string defaultFormat,
        bool enableNormalization,
        int normalizationLufs,
        bool emailNotifications,
        CancellationToken ct = default)
    {
        var settings = await _repo.GetByUserIdAsync(userId, ct)
            ?? new UserSettings { UserId = userId };

        settings.DefaultFormat = defaultFormat;
        settings.EnableNormalization = enableNormalization;
        settings.NormalizationLufs = normalizationLufs;
        settings.EmailNotifications = emailNotifications;
        settings.UpdatedAt = DateTime.UtcNow;

        await _repo.UpsertAsync(settings, ct);

        return settings;
    }
}
