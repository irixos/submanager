using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubManagerLite.Application.Entities;

namespace SubManagerLite.Data.EfConfigs;

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
            .Property(x => x.Handle)
            .HasMaxLength(30);
        
        builder
            .Property(x => x.Description)
            .HasMaxLength(5000);
        
        builder
            .Property(x => x.ThumbnailUrl)
            .HasMaxLength(1000);
        
        builder
            .Property(x => x.AddedDate)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()")
            .ValueGeneratedOnAdd();
        
        builder
            .HasIndex(x => x.YoutubeChannelId)
            .IsUnique();
    }
}