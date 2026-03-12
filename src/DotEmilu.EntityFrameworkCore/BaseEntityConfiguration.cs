namespace DotEmilu.EntityFrameworkCore;

/// <summary>
/// Configures mapping properties for entities implementing <see cref="IBaseEntity{TKey}"/>.
/// </summary>
/// <typeparam name="TBaseEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public sealed class BaseEntityConfiguration<TBaseEntity, TKey>(MappingStrategy strategy, bool enableRowVersion)
    : IEntityTypeConfiguration<TBaseEntity>
    where TBaseEntity : class, IBaseEntity<TKey>
    where TKey : struct
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TBaseEntity> builder)
    {
        builder
            .HasKey(s => s.Id);

        builder
            .Property(s => s.Id)
            .HasColumnOrder(0)
            .ValueGeneratedOnAdd();

        builder
            .UseIsDeleted(order: 1);

        if (enableRowVersion)
            builder
                .Property<byte[]>(nameof(Version))
                .IsRowVersion()
                .IsRequired();

        builder
            .ApplyMappingStrategy(strategy);
    }
}