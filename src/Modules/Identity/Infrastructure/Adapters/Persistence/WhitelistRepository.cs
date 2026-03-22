using Microsoft.EntityFrameworkCore;
using MusicGrabber.Modules.Identity.Application.Ports.Driven;
using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.Infrastructure.Adapters.Persistence;

internal sealed class WhitelistRepository : IWhitelistRepository
{
    private readonly IdentityDbContext _db;

    public WhitelistRepository(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<WhitelistEntry?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _db.WhitelistEntries
            .FirstOrDefaultAsync(e => e.UserId == userId, ct);
    }

    public async Task<IReadOnlyList<WhitelistEntry>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.WhitelistEntries
            .OrderBy(e => e.AddedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(WhitelistEntry entry, CancellationToken ct = default)
    {
        _db.WhitelistEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(WhitelistEntry entry, CancellationToken ct = default)
    {
        _db.WhitelistEntries.Update(entry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _db.WhitelistEntries.FindAsync([id], ct);
        if (entry is not null)
        {
            _db.WhitelistEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);
        }
    }
}
