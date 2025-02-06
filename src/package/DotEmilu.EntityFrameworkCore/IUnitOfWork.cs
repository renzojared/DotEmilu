namespace DotEmilu.EntityFrameworkCore;

public interface IUnitOfWork
{
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}