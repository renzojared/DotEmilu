namespace DotEmilu.Samples.ConsoleApp.Scenarios.S07OrchestratingHandler;

/// <summary>
/// Sub-handler that validates an order's business rules before it can be processed.
/// Resolved and invoked by <see cref="ProcessOrderHandler"/> — callers never interact
/// with this handler directly.
/// <para>
/// Demonstrates two validation layers working in concert:
/// <list type="bullet">
///   <item>
///     <b>FluentValidation</b> (via <see cref="ValidateOrderValidator"/>) rejects
///     structurally invalid requests before the use-case body runs.
///   </item>
///   <item>
///     <b>Runtime business rule</b> — orders above the approval threshold are
///     flagged via <see cref="IVerifier.AddValidationError(in string, in string)"/>
///     from inside the use-case, because the limit cannot be expressed as a static
///     FluentValidation rule.
///   </item>
/// </list>
/// </para>
/// </summary>
internal sealed class ValidateOrderHandler(IVerifier<ValidateOrderRequest> verifier)
    : Handler<ValidateOrderRequest>(verifier)
{
    private const decimal ApprovalThreshold = 10_000m;

    private readonly IVerifier<ValidateOrderRequest> _verifier = verifier;

    protected override Task HandleUseCaseAsync(ValidateOrderRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine(
            $"  🔎 [ValidateOrderHandler] Validating order '{request.OrderId}' — Amount: {request.TotalAmount:C}");

        // Business rule: orders exceeding the threshold require manual approval and
        // cannot be auto-processed.  This check lives here rather than in a validator
        // because the threshold could change at runtime (e.g. fetched from config/DB).
        if (request.TotalAmount > ApprovalThreshold)
        {
            _verifier.AddValidationError(
                nameof(request.TotalAmount),
                $"Orders above {ApprovalThreshold:C} require manual approval and cannot be auto-processed.");

            Console.WriteLine($"  ⛔ [ValidateOrderHandler] Amount {request.TotalAmount:C} exceeds approval threshold.");
            return Task.CompletedTask;
        }

        Console.WriteLine($"  ✅ [ValidateOrderHandler] Order '{request.OrderId}' passed all validations.");
        return Task.CompletedTask;
    }
}
