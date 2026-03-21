using Microsoft.EntityFrameworkCore;
using MusicGrabber.Modules.Quota.Domain;

namespace MusicGrabber.Modules.Quota.Infrastructure.Adapters.Persistence;

public sealed class QuotaDbContext : DbContext
{
    public QuotaDbContext(DbContextOptions<QuotaDbContext> options) : base(options)
    {
    }

    public DbSet<UserQuota> UserQuotas => Set<UserQuota>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserQuota>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.UserId).IsRequired().HasMaxLength(256);
            e.Property(x => x.CurrentThreshold).IsRequired().HasMaxLength(20);
            e.Property(x => x.LastEmailThreshold).HasMaxLength(20);
        });
    }
}
