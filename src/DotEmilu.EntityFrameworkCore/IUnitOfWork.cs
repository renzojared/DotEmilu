namespace DotEmilu.EntityFrameworkCore;

/// <summary>
/// Defines a contract for the Unit of Work pattern, exposing database capabilities and saving changes.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Gets the database facade for executing raw commands or transactions.</summary>
    DatabaseFacade Database { get; }

    /// <summary>Asynchronously saves all changes made in this unit of work to the database.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}