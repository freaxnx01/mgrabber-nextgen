using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.Application.Ports.Driving;

public interface IWhitelistService
{
    Task<IReadOnlyList<WhitelistEntry>> GetAllAsync(CancellationToken ct = default);
    Task<WhitelistEntry> AddAsync(string userId, string role, string addedBy, CancellationToken ct = default);
    Task<WhitelistEntry> ToggleAsync(Guid id, CancellationToken ct = default);
    Task RemoveAsync(Guid id, CancellationToken ct = default);
}
