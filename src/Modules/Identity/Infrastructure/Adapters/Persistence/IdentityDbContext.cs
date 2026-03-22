using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.Infrastructure.Adapters.Persistence;

public sealed class IdentityDbContext : IdentityDbContext<ApplicationUser>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<WhitelistEntry> WhitelistEntries => Set<WhitelistEntry>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<WhitelistEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.UserId).IsRequired().HasMaxLength(256);
            e.Property(x => x.Role).IsRequired().HasMaxLength(50);
            e.Property(x => x.AddedBy).IsRequired().HasMaxLength(256);
        });

        builder.Entity<UserSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.UserId).IsRequired().HasMaxLength(256);
            e.Property(x => x.DefaultFormat).IsRequired().HasMaxLength(20);
        });
    }
}
