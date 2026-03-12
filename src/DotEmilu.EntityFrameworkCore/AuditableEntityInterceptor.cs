namespace DotEmilu.EntityFrameworkCore;

/// <summary>
/// EF Core interceptor that automatically sets audit properties (Created, LastModified, Deleted, etc.)
/// based on the current user context.
/// </summary>
/// <typeparam name="TUserKey">The user key type (e.g., Guid or long).</typeparam>
public sealed class AuditableEntityInterceptor<TUserKey>(
    IContextUser<TUserKey> contextUser,
    TimeProvider timeProvider) : SaveChangesInterceptor
    where TUserKey : struct
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        SetAuditableData(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new())
    {
        SetAuditableData(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private readonly EntityState[] _entityStates =
    [
        EntityState.Added,
        EntityState.Modified,
        EntityState.Deleted
    ];

    private void SetAuditableData(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<IBaseAuditableEntity<TUserKey>>())
        {
            if (!_entityStates.Contains(entry.State) && !HasChangedOwnedEntities(entry)) continue;
            var utcNow = timeProvider.GetUtcNow();

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = contextUser.Id;
                    entry.Entity.Created = utcNow;
                    break;
                case EntityState.Modified when entry.Entity.IsDeleted:
                case EntityState.Deleted:
                    entry.State = EntityState.Unchanged;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedBy = contextUser.Id;
                    entry.Entity.Deleted = utcNow;
                    break;
            }

            entry.Entity.LastModifiedBy = contextUser.Id;
            entry.Entity.LastModified = utcNow;
        }

        return;

        bool HasChangedOwnedEntities(EntityEntry entry) =>
            entry.References.Any(r =>
                r.TargetEntry != null &&
                r.TargetEntry.Metadata.IsOwned() &&
                _entityStates.Contains(r.TargetEntry.State));
    }
}