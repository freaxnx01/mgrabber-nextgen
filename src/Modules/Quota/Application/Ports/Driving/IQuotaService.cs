using MusicGrabber.Modules.Quota.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Quota.Application.Ports.Driving;

/// <summary>Internal driving port for the Quota module.</summary>
public interface IQuotaService
{
    Task<QuotaInfoDto> GetQuotaAsync(string userId, CancellationToken ct = default);
    Task<bool> CheckAsync(string userId, long requiredBytes, CancellationToken ct = default);
    Task RecalculateUsageAsync(string userId, long usedBytes, int fileCount, CancellationToken ct = default);
    Task InitializeUserAsync(string userId, CancellationToken ct = default);
}
