using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubManager.Api.Application.Entities;

namespace SubManager.Api.Infrastructure.EfConfigs;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder
            .HasMany(c => c.Categories)
            .WithMany(c => c.Channels)
            .UsingEntity(e => e.ToTable("ChannelCategory"));
        
        builder
            .Property(c => c.YoutubeChannelId)
            .HasMaxLength(24);

        builder
            .Property(c => c.Name)
            .HasMaxLength(100);
        
        builder
            .Property(c => c.ThumbnailUrl)
            .HasMaxLength(1000);
        
        builder
            .Property(c => c.AddedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd();
        
        builder
            .Property(c => c.IsActive)
            .HasDefaultValue(true);       
        
        builder
            .HasIndex(c => c.YoutubeChannelId)
            .IsUnique();
    }
}