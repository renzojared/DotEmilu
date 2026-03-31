namespace DotEmilu.Samples.FullApp.Features.SyncInvoices;

internal sealed class ValidateSyncPreconditionsHandler : ChainHandler<SyncInvoicesContext>
{
    public override async Task ContinueAsync(SyncInvoicesContext chain, CancellationToken cancellationToken)
    {
        if (chain.TargetInvoiceIds.Length == 0)
        {
            chain.ValidationErrors.Add("No target invoices specified for synchronization.");
            return; // Short-circuit: do not call Successor
        }

        if (chain.TargetInvoiceIds.Length > 100)
        {
            chain.ValidationErrors.Add("Cannot sync more than 100 invoices at a time.");
            return; // Short-circuit
        }

        if (Successor is not null)
            await Successor.ContinueAsync(chain, cancellationToken);
    }
}
