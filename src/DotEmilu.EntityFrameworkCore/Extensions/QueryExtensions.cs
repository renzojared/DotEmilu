namespace DotEmilu.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extension methods for generic IQueryable types.
/// </summary>
public static class QueryExtensions
{
    /// <param name="source">The queryable source.</param>
    /// <typeparam name="T">The entity type.</typeparam>
    extension<T>(IQueryable<T> source) where T : class
    {
        /// <summary>Transforms an IQueryable into a paginated list using EF Core's CountAsync and ToListAsync.</summary>
        /// <param name="pageNumber">The index of the requested page (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A structured, paginated collection with pagination metadata.</returns>
        public async Task<PaginatedList<T>> AsPaginatedListAsync(int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var count = await source.CountAsync(cancellationToken);

            var items = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedList<T>(items, count, pageNumber, pageSize);
        }
    }
}