using MusicGrabber.Modules.Quota.Application.Ports.Driving;
using MusicGrabber.Shared.Contracts;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Quota.Infrastructure.Adapters;

internal sealed class QuotaFacadeAdapter : IQuotaFacade
{
    private readonly IQuotaService _service;

    public QuotaFacadeAdapter(IQuotaService service)
    {
        _service = service;
    }

    public Task<QuotaInfoDto> GetQuotaAsync(string userId, CancellationToken ct = default) =>
        _service.GetQuotaAsync(userId, ct);

    public Task<bool> CheckAsync(string userId, long requiredBytes, CancellationToken ct = default) =>
        _service.CheckAsync(userId, requiredBytes, ct);
}
