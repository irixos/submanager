using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubManagerLite.Application.Entities;

namespace SubManagerLite.Infrastructure.EfConfigs;

public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder
            .Property(x => x.YoutubeVideoId)
            .HasMaxLength(15);
        
        builder
            .Property(x => x.Title)
            .HasMaxLength(100);
        
        builder
            .Property(x => x.ThumbnailUrl)
            .HasMaxLength(1000);
        
        builder
            .Property(v => v.AddedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd();
        
        builder
            .Property(v => v.MetadataLastRefreshedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd();
        
        builder
            .HasIndex(x => x.YoutubeVideoId)
            .IsUnique();
        
        builder
            .HasIndex(x => x.ChannelId);
    }
    
}