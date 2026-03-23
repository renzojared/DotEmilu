namespace DotEmilu.Samples.ConsoleApp.Scenarios.S07OrchestratingHandler;

/// <summary>
/// Outer handler that orchestrates order processing by composing a sub-handler.
/// <para>
/// Demonstrates the <em>orchestrating handler</em> pattern:
/// <list type="number">
///   <item>
///     Inject <c>IHandler&lt;ValidateOrderRequest&gt;</c> and its companion
///     <c>IVerifier&lt;ValidateOrderRequest&gt;</c> as constructor dependencies.
///     Both are registered automatically because this handler lives in the same
///     assembly as <c>ValidateOrderHandler</c> and <c>ValidateOrderValidator</c>.
///   </item>
///   <item>
///     Build a sub-request from the outer request's data and call
///     <c>subHandler.HandleAsync</c>.
///   </item>
///   <item>
///     Check <c>subVerifier.IsValid</c> after the call — if the sub-handler added
///     any errors, copy them into the primary <c>_verifier</c> so the caller sees
///     a unified set of validation failures.
///   </item>
///   <item>
///     Only produce a <see cref="ProcessOrderResult"/> when the entire pipeline
///     succeeds.
///   </item>
/// </list>
/// </para>
/// </summary>
internal sealed class ProcessOrderHandler(
    IVerifier<ProcessOrderRequest> verifier,
    IHandler<ValidateOrderRequest> subHandler,
    IVerifier<ValidateOrderRequest> subVerifier)
    : Handler<ProcessOrderRequest, ProcessOrderResult>(verifier)
{
    private readonly IVerifier<ProcessOrderRequest> _verifier = verifier;

    protected override async Task<ProcessOrderResult?> HandleUseCaseAsync(
        ProcessOrderRequest request,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"  🔗 [ProcessOrderHandler] Orchestrating order '{request.OrderId}' for {request.CustomerEmail}…");

        // ── Step 1: delegate to the validation sub-handler ───────────────────
        var validateRequest = new ValidateOrderRequest(request.OrderId, request.TotalAmount);
        await subHandler.HandleAsync(validateRequest, cancellationToken);

        // ── Step 2: check sub-handler errors and propagate ───────────────────
        if (!subVerifier.IsValid)
        {
            // Copy every sub-handler error into the primary verifier so the caller
            // receives a single, consolidated list of validation failures.
            _verifier.AddValidationErrors(subVerifier.ValidationErrors.ToList());

            Console.WriteLine("  ❌ [ProcessOrderHandler] Sub-handler reported errors — aborting.");
            return null;
        }

        // ── Step 3: produce the result only when the pipeline fully succeeds ─
        var result = new ProcessOrderResult(
            OrderId:          request.OrderId,
            ConfirmationCode: $"CONF-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            ProcessedAt:      DateTimeOffset.UtcNow);

        Console.WriteLine($"  ✅ [ProcessOrderHandler] Order '{request.OrderId}' processed.");
        Console.WriteLine($"     Confirmation : {result.ConfirmationCode}");
        Console.WriteLine($"     Processed at : {result.ProcessedAt:u}");

        return result;
    }
}
