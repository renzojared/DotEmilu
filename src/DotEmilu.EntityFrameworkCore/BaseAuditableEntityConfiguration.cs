namespace DotEmilu.EntityFrameworkCore;

/// <summary>
/// Configures mapping properties for entities implementing <see cref="IBaseAuditableEntity{TUserKey}"/>.
/// </summary>
/// <typeparam name="TBaseAuditableEntity">The entity type.</typeparam>
/// <typeparam name="TUserKey">The user key type (e.g., Guid or long).</typeparam>
public sealed class BaseAuditableEntityConfiguration<TBaseAuditableEntity, TUserKey>(MappingStrategy strategy)
    : IEntityTypeConfiguration<TBaseAuditableEntity>
    where TBaseAuditableEntity : class, IBaseAuditableEntity<TUserKey>
    where TUserKey : struct
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TBaseAuditableEntity> builder)
    {
        builder
            .HasNoKey();

        builder
            .Property(s => s.Created)
            .IsRequired();

        builder
            .Property(s => s.CreatedBy)
            .IsRequired();

        builder
            .Property(s => s.LastModified)
            .IsRequired();

        builder
            .Property(s => s.LastModifiedBy)
            .IsRequired();

        builder
            .Property(s => s.Deleted)
            .IsRequired(false);

        builder
            .Property(s => s.DeletedBy)
            .IsRequired(false);

        builder
            .UseIsDeleted();

        builder
            .ApplyMappingStrategy(strategy);
    }
}