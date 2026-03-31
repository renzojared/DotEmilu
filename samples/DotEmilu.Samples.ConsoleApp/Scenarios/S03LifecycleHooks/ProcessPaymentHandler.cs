namespace DotEmilu.Samples.ConsoleApp.Scenarios.S03LifecycleHooks;

/// <summary>
/// Handles payment processing for an invoice.
/// Demonstrates the two lifecycle hooks available in <see cref="BaseHandler"/>:
/// <list type="bullet">
///   <item>
///     <see cref="HandleExceptionAsync"/> — called whenever the use-case throws;
///     use it for logging, alerting, or compensating actions.
///   </item>
///   <item>
///     <see cref="FinalizeAsync"/> — always called (success <em>or</em> failure),
///     equivalent to a <c>finally</c> block; ideal for cleanup and telemetry.
///   </item>
/// </list>
/// </summary>
internal sealed class ProcessPaymentHandler(IVerifier<ProcessPaymentRequest> verifier)
    : Handler<ProcessPaymentRequest>(verifier)
{
    protected override Task HandleUseCaseAsync(ProcessPaymentRequest request, CancellationToken cancellationToken)
    {
        // Simulate a business rule that rejects amounts over the processing limit.
        if (request.Amount > 50_000m)
            throw new InvalidOperationException(
                $"Amount {request.Amount:C} ({request.Currency}) exceeds the processing limit of 50,000.");

        Console.WriteLine(
            $"  ✅ Payment processed: invoice {request.InvoiceId} — {request.Amount:C} {request.Currency}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked when <see cref="HandleUseCaseAsync"/> throws an unhandled exception.
    /// The exception is still re-thrown after this method completes.
    /// </summary>
    protected override Task HandleExceptionAsync(Exception e)
    {
        Console.WriteLine($"  ⚠️  [HandleExceptionAsync] {e.GetType().Name}: {e.Message}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Always invoked after the handler completes, regardless of success or failure.
    /// </summary>
    protected override Task FinalizeAsync()
    {
        Console.WriteLine("  🏁 [FinalizeAsync] Handler finalized (always runs).");
        return Task.CompletedTask;
    }
}
