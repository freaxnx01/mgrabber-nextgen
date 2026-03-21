using Microsoft.EntityFrameworkCore;
using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Download.Infrastructure.Adapters.Persistence;

public sealed class DownloadJobRepository(DownloadDbContext db) : IDownloadJobRepository
{
    public Task<DownloadJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.DownloadJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<List<DownloadJob>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => db.DownloadJobs.Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

    public Task<List<DownloadJob>> GetCompletedByUserIdAsync(string userId, CancellationToken ct = default)
        => db.DownloadJobs.Where(j => j.UserId == userId && j.Status == DownloadStatus.Completed)
            .OrderByDescending(j => j.CompletedAt)
            .ToListAsync(ct);

    public Task<int> GetActiveCountByUserIdAsync(string userId, CancellationToken ct = default)
        => db.DownloadJobs.CountAsync(j => j.UserId == userId &&
            (j.Status == DownloadStatus.Downloading || j.Status == DownloadStatus.Normalizing || j.Status == DownloadStatus.Pending), ct);

    public Task<int> GetActiveCountAsync(CancellationToken ct = default)
        => db.DownloadJobs.CountAsync(j =>
            j.Status == DownloadStatus.Downloading || j.Status == DownloadStatus.Normalizing, ct);

    public async Task AddAsync(DownloadJob job, CancellationToken ct = default)
    {
        db.DownloadJobs.Add(job);
        await db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(DownloadJob job, CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var job = await db.DownloadJobs.FindAsync([id], ct);
        if (job is not null)
        {
            db.DownloadJobs.Remove(job);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<UserStatsDto> GetUserStatsAsync(string userId, CancellationToken ct = default)
    {
        var jobs = await db.DownloadJobs.Where(j => j.UserId == userId).ToListAsync(ct);
        var topArtists = jobs
            .Where(j => !string.IsNullOrEmpty(j.Author))
            .GroupBy(j => j.Author!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());
        var downloadsPerDay = jobs
            .Where(j => j.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(j => j.CreatedAt.ToString("yyyy-MM-dd"))
            .ToDictionary(g => g.Key, g => g.Count());

        return new UserStatsDto(
            UserId: userId,
            TotalDownloads: jobs.Count,
            CompletedDownloads: jobs.Count(j => j.Status == DownloadStatus.Completed),
            FailedDownloads: jobs.Count(j => j.Status == DownloadStatus.Failed),
            TotalStorageBytes: jobs.Where(j => j.Status == DownloadStatus.Completed).Sum(j => j.FileSizeBytes),
            LastActive: jobs.MaxBy(j => j.UpdatedAt)?.UpdatedAt,
            TopArtists: topArtists,
            DownloadsPerDay: downloadsPerDay);
    }

    public async Task<GlobalStatsDto> GetGlobalStatsAsync(CancellationToken ct = default)
    {
        var jobs = await db.DownloadJobs.ToListAsync(ct);
        var statusCounts = jobs.GroupBy(j => j.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
        var downloadsPerDay = jobs.Where(j => j.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(j => j.CreatedAt.ToString("yyyy-MM-dd"))
            .ToDictionary(g => g.Key, g => g.Count());
        var userStats = jobs.GroupBy(j => j.UserId)
            .Select(g =>
            {
                var topArtists = g
                    .Where(j => !string.IsNullOrEmpty(j.Author))
                    .GroupBy(j => j.Author!)
                    .OrderByDescending(gr => gr.Count())
                    .Take(10)
                    .ToDictionary(gr => gr.Key, gr => gr.Count());
                var perDay = g
                    .Where(j => j.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                    .GroupBy(j => j.CreatedAt.ToString("yyyy-MM-dd"))
                    .ToDictionary(gr => gr.Key, gr => gr.Count());
                return new UserStatsDto(
                    g.Key, g.Count(),
                    g.Count(j => j.Status == DownloadStatus.Completed),
                    g.Count(j => j.Status == DownloadStatus.Failed),
                    g.Where(j => j.Status == DownloadStatus.Completed).Sum(j => j.FileSizeBytes),
                    g.MaxBy(j => j.UpdatedAt)?.UpdatedAt,
                    topArtists,
                    perDay);
            })
            .ToList();
        var activeUsers = jobs.Where(j => j.UpdatedAt >= DateTime.UtcNow.AddDays(-7))
            .Select(j => j.UserId).Distinct().Count();

        return new GlobalStatsDto(
            jobs.Count,
            jobs.Where(j => j.Status == DownloadStatus.Completed).Sum(j => j.FileSizeBytes),
            activeUsers, statusCounts, downloadsPerDay, userStats);
    }

    public Task<long> GetTotalFileSizeByUserIdAsync(string userId, CancellationToken ct = default)
        => db.DownloadJobs.Where(j => j.UserId == userId && j.Status == DownloadStatus.Completed)
            .SumAsync(j => j.FileSizeBytes, ct);

    public Task<int> GetFileCountByUserIdAsync(string userId, CancellationToken ct = default)
        => db.DownloadJobs.CountAsync(j => j.UserId == userId && j.Status == DownloadStatus.Completed, ct);
}
