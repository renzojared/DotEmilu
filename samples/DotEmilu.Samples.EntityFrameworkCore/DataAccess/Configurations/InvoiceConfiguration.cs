using DotEmilu.Samples.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotEmilu.Samples.EntityFrameworkCore.DataAccess.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Invoice"/>.
/// Applies custom property constraints. Base properties are configured by BaseAuditableEntityConfiguration.
/// </summary>
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder
            .Property(i => i.Number)
            .HasMaxLength(20)
            .IsRequired();

        builder
            .Property(i => i.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(i => i.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder
            .Property(i => i.Date)
            .IsRequired();

        builder
            .Property(i => i.IsPaid)
            .HasDefaultValue(false);
    }
}
