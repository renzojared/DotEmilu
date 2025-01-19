namespace DotEmilu.EntityFrameworkCore;

public class AuditableEntityInterceptor(IContextUser contextUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        SetAuditableData(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

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
        EntityState.Modified
    ];

    private void SetAuditableData(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
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
                    entry.Entity.DeletedBy = contextUser.Id;
                    entry.Entity.Deleted = utcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
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