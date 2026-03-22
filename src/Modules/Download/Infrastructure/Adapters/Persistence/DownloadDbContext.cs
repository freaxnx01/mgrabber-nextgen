using Microsoft.EntityFrameworkCore;
using MusicGrabber.Modules.Download.Domain;

namespace MusicGrabber.Modules.Download.Infrastructure.Adapters.Persistence;

public sealed class DownloadDbContext(DbContextOptions<DownloadDbContext> options) : DbContext(options)
{
    public DbSet<DownloadJob> DownloadJobs => Set<DownloadJob>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(DownloadDbContext).Assembly);
    }
}
