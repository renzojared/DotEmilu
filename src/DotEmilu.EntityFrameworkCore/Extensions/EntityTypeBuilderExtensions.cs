namespace DotEmilu.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extension members for EF Core's <see cref="EntityTypeBuilder{T}"/>.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>Extension members for <see cref="EntityTypeBuilder{T}"/> where T implements <see cref="IBaseEntity"/>.</summary>
    extension<T>(EntityTypeBuilder<T> builder) where T : class, IBaseEntity
    {
        /// <summary>Applies a specific mapping strategy (Tpc, Tph, Tpt) to the entity.</summary>
        /// <param name="strategy">The mapping strategy to apply.</param>
        /// <returns>The same builder instance for chaining.</returns>
        public EntityTypeBuilder<T> ApplyMappingStrategy(MappingStrategy strategy)
            => strategy switch
            {
                MappingStrategy.Tph => builder.UseTphMappingStrategy(),
                MappingStrategy.Tpt => builder.UseTptMappingStrategy(),
                _ => builder.UseTpcMappingStrategy()
            };

        /// <summary>Configures the <c>IsDeleted</c> property for soft-delete functionality.</summary>
        /// <param name="useShort">If <see langword="true"/>, stores as <c>short</c> (0/1) instead of <see langword="bool"/>.</param>
        /// <param name="useIndex">Whether to create an index on the <c>IsDeleted</c> column.</param>
        /// <param name="useQueryFilter">Whether to add a global query filter to exclude deleted records.</param>
        /// <param name="order">Column order (0 = no specific order).</param>
        /// <returns>The same builder instance for chaining.</returns>
        public EntityTypeBuilder<T> UseIsDeleted(
            bool useShort = false,
            bool useIndex = true,
            bool useQueryFilter = true,
            int order = 0)
        {
            var propertyBuilder = builder.Property(s => s.IsDeleted);

            if (useShort)
            {
                propertyBuilder
                    .HasDefaultValue(0)
                    .HasShortConversion()
                    .HasComment("Soft delete: 1 is deleted")
                    .IsRequired();
            }
            else
            {
                propertyBuilder
                    .HasDefaultValue(false)
                    .HasComment("Soft delete: true is deleted")
                    .IsRequired();
            }

            if (order > 0)
                propertyBuilder.HasColumnOrder(order);

            if (useQueryFilter)
                builder.HasQueryFilter(s => !s.IsDeleted);

            if (useIndex)
                builder.HasIndex(s => s.IsDeleted);

            return builder;
        }
    }
}