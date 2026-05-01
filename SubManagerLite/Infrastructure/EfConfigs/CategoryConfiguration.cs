using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubManagerLite.Application.Entities;

namespace SubManagerLite.Infrastructure.EfConfigs;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder
            .Property(c => c.Name)
            .HasMaxLength(50);

        builder
            .Property(c => c.Color)
            .HasMaxLength(7);
        
        builder
            .HasIndex(c => c.Name)
            .IsUnique();
    }
}