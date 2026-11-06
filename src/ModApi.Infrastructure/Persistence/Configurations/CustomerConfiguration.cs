using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModApi.Domain.Entities;

namespace ModApi.Infrastructure.Persistence.Configurations;

/// <summary>Fluent configuration for <see cref="Customer"/>.</summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
