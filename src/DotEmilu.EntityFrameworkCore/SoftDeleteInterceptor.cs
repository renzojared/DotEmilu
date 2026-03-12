namespace DotEmilu.EntityFrameworkCore;

/// <summary>
/// EF Core interceptor that converts entity deletions into soft deletes by changing
/// their state and updating the <see cref="IBaseEntity.IsDeleted"/> flag.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        SetSoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new())
    {
        SetSoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SetSoftDelete(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<IBaseEntity>())
        {
            if (entry.State != EntityState.Deleted) continue;

            entry.State = EntityState.Unchanged;
            entry.Entity.IsDeleted = true;
        }
    }
}