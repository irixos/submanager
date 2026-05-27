using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubManager.Api.Application.Entities;

namespace SubManager.Api.Infrastructure.EfConfigs;

public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder
            .Property(v => v.YoutubeVideoId)
            .HasMaxLength(15);
        
        builder
            .Property(v => v.Title)
            .HasMaxLength(100);
        
        builder
            .Property(v => v.ThumbnailUrl)
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
            .HasIndex(v => v.YoutubeVideoId)
            .IsUnique();
        
        builder
            .HasIndex(v => v.ChannelId);
    }
    
}