namespace DotEmilu.Samples.ConsoleApp.Scenarios.S06ChainHandlerMultiStep;

/// <summary>
/// Orchestrates the three-step synchronization chain by wiring the links together
/// in the constructor and exposing the pipeline as a single
/// <see cref="IHandler{TRequest}"/> entry-point.
/// <para>
/// This is the <em>Processor</em> pattern from the DotEmilu playbook:
/// <list type="number">
///   <item>
///     Inject each chain handler as a constructor dependency — DI resolves them
///     because <c>AddChainHandlers(assembly)</c> registers every concrete
///     <see cref="ChainHandler{TChain}"/> by its concrete type.
///   </item>
///   <item>
///     Wire the chain in the constructor with
///     <c>a.SetSuccessor(b).SetSuccessor(c)</c> — the fluent overload returns the
///     successor, so the whole chain is expressed in one readable statement.
///   </item>
///   <item>
///     Implement <c>IHandler&lt;SyncJobContext&gt;</c> directly (no base
///     <c>Handler&lt;T&gt;</c>) so the processor owns its own error handling
///     without requiring a verifier for the pipeline context itself.
///   </item>
/// </list>
/// </para>
/// <remarks>
/// Callers resolve <c>IHandler&lt;SyncJobContext&gt;</c> from DI and call
/// <c>HandleAsync</c> — they never see the individual chain steps.
/// </remarks>
/// </summary>
internal sealed class SyncJobProcessor : IHandler<SyncJobContext>
{
    private readonly ValidateJobHandler _validateJobHandler;

    public SyncJobProcessor(
        ValidateJobHandler validateJobHandler,
        EnrichJobHandler enrichJobHandler,
        PersistJobHandler persistJobHandler)
    {
        // Wire the chain once during construction.
        // SetSuccessor returns the successor, enabling fluent chaining:
        //   validateJobHandler → enrichJobHandler → persistJobHandler
        validateJobHandler
            .SetSuccessor(enrichJobHandler)
            .SetSuccessor(persistJobHandler);

        _validateJobHandler = validateJobHandler;
    }

    /// <inheritdoc />
    public async Task HandleAsync(SyncJobContext request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  🚀 [SyncJobProcessor] Starting pipeline for job '{request.JobId}'…");

        await _validateJobHandler.ContinueAsync(request, cancellationToken);

        Console.WriteLine($"  🏁 [SyncJobProcessor] Pipeline finished. IsPersisted={request.IsPersisted}");

        if (request.ValidationErrors.Count > 0)
        {
            Console.WriteLine($"  ⚠️  Validation issues ({request.ValidationErrors.Count}):");
            foreach (var error in request.ValidationErrors)
                Console.WriteLine($"     • {error}");
        }
    }
}
