using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.Application.Ports.Driven;

public interface IWhitelistRepository
{
    Task<WhitelistEntry?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<WhitelistEntry>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(WhitelistEntry entry, CancellationToken ct = default);
    Task UpdateAsync(WhitelistEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
