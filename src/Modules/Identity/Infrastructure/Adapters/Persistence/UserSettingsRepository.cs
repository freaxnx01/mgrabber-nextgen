using Microsoft.EntityFrameworkCore;
using MusicGrabber.Modules.Identity.Application.Ports.Driven;
using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.Infrastructure.Adapters.Persistence;

internal sealed class UserSettingsRepository : IUserSettingsRepository
{
    private readonly IdentityDbContext _db;

    public UserSettingsRepository(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<UserSettings?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _db.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);
    }

    public async Task UpsertAsync(UserSettings settings, CancellationToken ct = default)
    {
        var existing = await _db.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == settings.UserId, ct);

        if (existing is null)
        {
            _db.UserSettings.Add(settings);
        }
        else
        {
            existing.DefaultFormat = settings.DefaultFormat;
            existing.EnableNormalization = settings.EnableNormalization;
            existing.NormalizationLufs = settings.NormalizationLufs;
            existing.EmailNotifications = settings.EmailNotifications;
            existing.UpdatedAt = settings.UpdatedAt;
            _db.UserSettings.Update(existing);
        }

        await _db.SaveChangesAsync(ct);
    }
}
