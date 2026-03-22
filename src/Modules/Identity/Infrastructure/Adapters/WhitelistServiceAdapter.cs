using MusicGrabber.Modules.Identity.Application.Ports.Driving;
using MusicGrabber.Modules.Identity.Application.UseCases.ManageWhitelist;
using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.Infrastructure.Adapters;

internal sealed class WhitelistServiceAdapter : IWhitelistService
{
    private readonly ManageWhitelistHandler _handler;

    public WhitelistServiceAdapter(ManageWhitelistHandler handler)
    {
        _handler = handler;
    }

    public Task<IReadOnlyList<WhitelistEntry>> GetAllAsync(CancellationToken ct = default) =>
        _handler.GetAllAsync(ct);

    public Task<WhitelistEntry> AddAsync(string userId, string role, string addedBy, CancellationToken ct = default) =>
        _handler.AddAsync(userId, role, addedBy, ct);

    public Task<WhitelistEntry> ToggleAsync(Guid id, CancellationToken ct = default) =>
        _handler.ToggleAsync(id, ct);

    public Task RemoveAsync(Guid id, CancellationToken ct = default) =>
        _handler.RemoveAsync(id, ct);
}
