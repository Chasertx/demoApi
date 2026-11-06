using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModApi.Domain.Entities;

namespace ModApi.Infrastructure.Persistence.Configurations;

/// <summary>Fluent configuration that cannot be expressed with data annotations.</summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
