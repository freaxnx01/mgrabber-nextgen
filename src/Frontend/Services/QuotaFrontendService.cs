using MusicGrabber.Modules.Quota.Application.Ports.Driving;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Services;

public sealed class QuotaFrontendService(IQuotaService quotaService) : IQuotaFrontendService
{
    public Task<QuotaInfoDto> GetQuotaAsync(string userId, CancellationToken ct)
        => quotaService.GetQuotaAsync(userId, ct);

    public Task<bool> CheckAsync(string userId, long requiredBytes, CancellationToken ct)
        => quotaService.CheckAsync(userId, requiredBytes, ct);
}
