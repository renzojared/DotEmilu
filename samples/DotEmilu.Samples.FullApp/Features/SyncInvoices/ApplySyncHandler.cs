namespace DotEmilu.Samples.FullApp.Features.SyncInvoices;

internal sealed class ApplySyncHandler(IInvoiceCommands db) : ChainHandler<SyncInvoicesContext>
{
    public override async Task ContinueAsync(SyncInvoicesContext chain, CancellationToken cancellationToken)
    {
        foreach (var invoice in chain.ValidatedInvoices)
        {
            // Simulate modifying state via a "sync"
            invoice.Description = $"{invoice.Description} [Synced {DateTimeOffset.UtcNow:O}]";
            chain.SyncedCount++;
        }

        // Persist the changes
        await db.SaveChangesAsync(cancellationToken);
        chain.IsPersisted = true;

        if (Successor is not null)
            await Successor.ContinueAsync(chain, cancellationToken);
    }
}
