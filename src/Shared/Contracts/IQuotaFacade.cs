using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Shared.Contracts;

public interface IQuotaFacade
{
    Task<QuotaInfoDto> GetQuotaAsync(string userId, CancellationToken ct = default);
    Task<bool> CheckAsync(string userId, long requiredBytes, CancellationToken ct = default);
}
