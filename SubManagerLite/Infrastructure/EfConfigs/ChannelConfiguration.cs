using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubManagerLite.Application.Entities;

namespace SubManagerLite.Infrastructure.EfConfigs;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder
            .HasMany(x => x.Categories)
            .WithMany(x => x.Channels)
            .UsingEntity(x => x.ToTable("ChannelCategory"));
        
        builder
            .Property(x => x.YoutubeChannelId)
            .HasMaxLength(24);

        builder
            .Property(x => x.Name)
            .HasMaxLength(100);
        
        builder
            .Property(x => x.ThumbnailUrl)
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