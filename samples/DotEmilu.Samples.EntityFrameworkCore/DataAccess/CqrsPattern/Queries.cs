using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.EntityFrameworkCore.DataAccess.CqrsPattern;

/// <summary>
/// Read-side DbContext implementation with no-tracking behavior.
/// All queries are read-only, improving performance for read-heavy scenarios.
/// </summary>
internal sealed class Queries : InvoiceDbContext, IQueries
{
    public Queries(DbContextOptions<InvoiceDbContext> options) : base(options)
        => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
}
