using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.FullApp.Infrastructure.Persistence;

internal sealed class InvoiceCommands(DbContextOptions<InvoiceDbContext> options)
    : InvoiceDbContext(options), IInvoiceCommands;

internal sealed class InvoiceQueries : InvoiceDbContext, IInvoiceQueries
{
    public InvoiceQueries(DbContextOptions<InvoiceDbContext> options) : base(options)
        => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
}
