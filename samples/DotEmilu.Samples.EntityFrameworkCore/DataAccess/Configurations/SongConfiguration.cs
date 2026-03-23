using DotEmilu.EntityFrameworkCore;
using DotEmilu.EntityFrameworkCore.Extensions;
using DotEmilu.Samples.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotEmilu.Samples.EntityFrameworkCore.DataAccess.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Song"/>.
/// Demonstrates <c>UseIsDeleted(useShort: true)</c> to override default soft delete boolean,
/// and <c>HasFormattedComment()</c>.
/// </summary>
public class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder
            .Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder
            .Property(s => s.Type)
            .HasFormattedComment("{0} = {2}", includeTitle: true)
            .IsRequired();

        builder
            .UseIsDeleted(useShort: true, useIndex: true, useQueryFilter: true, order: 1);

        // Per-entity override example. With no hierarchy this remains intent metadata,
        // but it demonstrates the explicit override API from EntityTypeBuilderExtensions.
        builder.ApplyMappingStrategy(MappingStrategy.Tpt);
    }
}
