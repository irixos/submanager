using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubManagerLite.Application.Entities;

namespace SubManagerLite.Infrastructure.EfConfigs;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder
            .Property(x => x.Name)
            .HasMaxLength(50);

        builder
            .Property(x => x.Color)
            .HasMaxLength(7);
        
        builder
            .HasIndex(x => x.Name)
            .IsUnique();
    }
}