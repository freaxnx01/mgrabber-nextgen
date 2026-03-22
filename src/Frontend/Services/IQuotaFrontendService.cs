using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Services;

public interface IQuotaFrontendService
{
    Task<QuotaInfoDto> GetQuotaAsync(string userId, CancellationToken ct = default);
    Task<bool> CheckAsync(string userId, long requiredBytes, CancellationToken ct = default);
}
