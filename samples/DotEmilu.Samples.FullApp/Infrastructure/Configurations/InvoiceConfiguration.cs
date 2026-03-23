using DotEmilu.Samples.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotEmilu.Samples.FullApp.Infrastructure.Configurations;

/// <summary>
/// Business-specific EF Core configuration for <see cref="Invoice"/>.
/// Base entity and auditable behavior are applied by ModelBuilder extension pipeline in InvoiceDbContext.
/// </summary>
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.Property(i => i.Number).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.Date).IsRequired();
        builder.Property(i => i.IsPaid).HasDefaultValue(false);
    }
}
