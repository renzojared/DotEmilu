using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.FullApp.Features.SyncInvoices;

internal sealed class LoadTargetInvoicesHandler(IInvoiceCommands db) : ChainHandler<SyncInvoicesContext>
{
    public override async Task ContinueAsync(SyncInvoicesContext chain, CancellationToken cancellationToken)
    {
        var invoices = await db.Invoices
            .Where(i => chain.TargetInvoiceIds.AsEnumerable().Contains(i.Id))
            .ToListAsync(cancellationToken);

        if (invoices.Count == 0)
        {
            chain.ValidationErrors.Add("None of the specified invoices were found in the database.");
            return; // Short-circuit
        }

        // Add to tracked validated list
        chain.ValidatedInvoices.AddRange(invoices);

        if (Successor is not null)
            await Successor.ContinueAsync(chain, cancellationToken);
    }
}
