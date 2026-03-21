using MusicGrabber.Modules.Identity.Application.Ports.Driven;
using MusicGrabber.Modules.Identity.Domain;
using MusicGrabber.Shared;
using MusicGrabber.Shared.Events;

namespace MusicGrabber.Modules.Identity.Application.UseCases.ManageWhitelist;

public sealed class ManageWhitelistHandler
{
    private readonly IWhitelistRepository _repo;
    private readonly IEventBus _eventBus;

    public ManageWhitelistHandler(IWhitelistRepository repo, IEventBus eventBus)
    {
        _repo = repo;
        _eventBus = eventBus;
    }

    public async Task<IReadOnlyList<WhitelistEntry>> GetAllAsync(CancellationToken ct = default)
    {
        return await _repo.GetAllAsync(ct);
    }

    public async Task<WhitelistEntry> AddAsync(string userId, string role, string addedBy, CancellationToken ct = default)
    {
        var entry = new WhitelistEntry
        {
            UserId = userId,
            Role = role,
            AddedBy = addedBy,
            IsActive = true
        };

        await _repo.AddAsync(entry, ct);
        await _eventBus.PublishAsync(new UserWhitelistedEvent(userId), ct);

        return entry;
    }

    public async Task<WhitelistEntry> ToggleAsync(Guid id, CancellationToken ct = default)
    {
        var entries = await _repo.GetAllAsync(ct);
        var entry = entries.FirstOrDefault(e => e.Id == id)
            ?? throw new InvalidOperationException($"Whitelist entry {id} not found.");

        entry.IsActive = !entry.IsActive;
        await _repo.UpdateAsync(entry, ct);

        return entry;
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        await _repo.DeleteAsync(id, ct);
    }
}
