using Microsoft.EntityFrameworkCore;
using MusicGrabber.Modules.Quota.Application.Ports.Driven;
using MusicGrabber.Modules.Quota.Domain;

namespace MusicGrabber.Modules.Quota.Infrastructure.Adapters.Persistence;

internal sealed class QuotaRepository : IQuotaRepository
{
    private readonly QuotaDbContext _db;

    public QuotaRepository(QuotaDbContext db)
    {
        _db = db;
    }

    public async Task<UserQuota?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _db.UserQuotas
            .FirstOrDefaultAsync(q => q.UserId == userId, ct);
    }

    public async Task AddAsync(UserQuota quota, CancellationToken ct = default)
    {
        _db.UserQuotas.Add(quota);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserQuota quota, CancellationToken ct = default)
    {
        _db.UserQuotas.Update(quota);
        await _db.SaveChangesAsync(ct);
    }
}
