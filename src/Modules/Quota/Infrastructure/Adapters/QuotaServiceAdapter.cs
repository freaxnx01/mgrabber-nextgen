using MusicGrabber.Modules.Quota.Application.Ports.Driving;
using MusicGrabber.Modules.Quota.Application.UseCases.CheckQuota;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Quota.Infrastructure.Adapters;

internal sealed class QuotaServiceAdapter : IQuotaService
{
    private readonly CheckQuotaHandler _handler;

    public QuotaServiceAdapter(CheckQuotaHandler handler)
    {
        _handler = handler;
    }

    public Task<QuotaInfoDto> GetQuotaAsync(string userId, CancellationToken ct = default) =>
        _handler.GetQuotaAsync(userId, ct);

    public Task<bool> CheckAsync(string userId, long requiredBytes, CancellationToken ct = default) =>
        _handler.CheckAsync(userId, requiredBytes, ct);

    public Task RecalculateUsageAsync(string userId, long usedBytes, int fileCount, CancellationToken ct = default) =>
        _handler.RecalculateUsageAsync(userId, usedBytes, fileCount, ct);

    public Task InitializeUserAsync(string userId, CancellationToken ct = default) =>
        _handler.InitializeUserAsync(userId, ct);
}
