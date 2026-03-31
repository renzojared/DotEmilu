using DotEmilu.Abstractions;

namespace DotEmilu.Samples.FullApp.Features.SyncInvoices;

internal sealed class SyncInvoicesProcessor : IHandler<SyncInvoicesContext>
{
    private readonly ValidateSyncPreconditionsHandler _validateHandler;

    public SyncInvoicesProcessor(
        ValidateSyncPreconditionsHandler validateHandler,
        LoadTargetInvoicesHandler loadHandler,
        ApplySyncHandler applyHandler)
    {
        // Wire the chain fluently
        validateHandler
            .SetSuccessor(loadHandler)
            .SetSuccessor(applyHandler);

        _validateHandler = validateHandler;
    }

    public async Task HandleAsync(SyncInvoicesContext request, CancellationToken cancellationToken)
    {
        await _validateHandler.ContinueAsync(request, cancellationToken);
    }
}
