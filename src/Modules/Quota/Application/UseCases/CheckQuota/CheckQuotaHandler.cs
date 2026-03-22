using MusicGrabber.Modules.Quota.Application.Ports.Driven;
using MusicGrabber.Modules.Quota.Domain;
using MusicGrabber.Shared;
using MusicGrabber.Shared.DTOs;
using MusicGrabber.Shared.Events;

namespace MusicGrabber.Modules.Quota.Application.UseCases.CheckQuota;

public sealed class CheckQuotaHandler
{
    private readonly IQuotaRepository _repo;
    private readonly IEventBus _eventBus;

    public CheckQuotaHandler(IQuotaRepository repo, IEventBus eventBus)
    {
        _repo = repo;
        _eventBus = eventBus;
    }

    public async Task<QuotaInfoDto> GetQuotaAsync(string userId, CancellationToken ct = default)
    {
        var quota = await _repo.GetByUserIdAsync(userId, ct)
            ?? throw new InvalidOperationException($"Quota record not found for user '{userId}'.");

        return ToDto(quota);
    }

    public async Task<bool> CheckAsync(string userId, long requiredBytes, CancellationToken ct = default)
    {
        var quota = await _repo.GetByUserIdAsync(userId, ct);
        if (quota is null)
            return false;

        return quota.HasSpaceFor(requiredBytes);
    }

    /// <summary>
    /// Recalculates storage usage for <paramref name="userId"/> using the provided file totals.
    /// Publishes a <see cref="QuotaThresholdCrossedEvent"/> if the threshold changes.
    /// </summary>
    public async Task RecalculateUsageAsync(
        string userId,
        long newUsedBytes,
        int newFileCount,
        CancellationToken ct = default)
    {
        var quota = await _repo.GetByUserIdAsync(userId, ct);
        if (quota is null)
            return;

        var previousThreshold = quota.CurrentThreshold;
        quota.UsedBytes = newUsedBytes;
        quota.FileCount = newFileCount;
        quota.CurrentThreshold = UserQuota.CalculateThreshold(newUsedBytes, quota.QuotaBytes);
        quota.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(quota, ct);

        if (quota.CurrentThreshold != previousThreshold)
        {
            await _eventBus.PublishAsync(
                new QuotaThresholdCrossedEvent(userId, quota.CurrentThreshold), ct);
        }
    }

    public async Task InitializeUserAsync(string userId, CancellationToken ct = default)
    {
        var existing = await _repo.GetByUserIdAsync(userId, ct);
        if (existing is not null)
            return;

        var quota = new UserQuota
        {
            UserId = userId,
            QuotaBytes = UserQuota.DefaultQuotaBytes,
            UsedBytes = 0L,
            FileCount = 0,
            CurrentThreshold = QuotaThreshold.Normal
        };

        await _repo.AddAsync(quota, ct);
    }

    private static QuotaInfoDto ToDto(UserQuota quota) =>
        new(quota.UserId, quota.QuotaBytes, quota.UsedBytes, quota.FileCount, quota.CurrentThreshold);
}
