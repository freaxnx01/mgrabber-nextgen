using MusicGrabber.Modules.Quota.Domain;

namespace MusicGrabber.Modules.Quota.Application.Ports.Driven;

public interface IQuotaRepository
{
    Task<UserQuota?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(UserQuota quota, CancellationToken ct = default);
    Task UpdateAsync(UserQuota quota, CancellationToken ct = default);
}
