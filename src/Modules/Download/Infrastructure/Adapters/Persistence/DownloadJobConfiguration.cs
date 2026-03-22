using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MusicGrabber.Modules.Download.Domain;

namespace MusicGrabber.Modules.Download.Infrastructure.Adapters.Persistence;

public sealed class DownloadJobConfiguration : IEntityTypeConfiguration<DownloadJob>
{
    public void Configure(EntityTypeBuilder<DownloadJob> builder)
    {
        builder.HasKey(j => j.Id);
        builder.HasIndex(j => j.UserId);
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.VideoId);
        builder.HasIndex(j => j.PlaylistId);
        builder.Property(j => j.UserId).IsRequired().HasMaxLength(256);
        builder.Property(j => j.Url).IsRequired().HasMaxLength(2048);
        builder.Property(j => j.Format).HasConversion<string>().HasMaxLength(10);
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(j => j.Title).HasMaxLength(500);
        builder.Property(j => j.Author).HasMaxLength(256);
    }
}
