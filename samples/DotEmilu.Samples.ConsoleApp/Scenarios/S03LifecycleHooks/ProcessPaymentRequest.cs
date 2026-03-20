namespace DotEmilu.Samples.ConsoleApp.Scenarios.S03LifecycleHooks;

/// <summary>
/// Request to process a payment for an invoice.
/// Defined locally in this scenario so that <c>ProcessPaymentHandler</c>
/// is the sole implementation of <c>IHandler&lt;ProcessPaymentRequest&gt;</c>
/// in the assembly, enabling clean resolution via <c>AddHandlers</c>.
/// </summary>
public sealed record ProcessPaymentRequest(
    string InvoiceId,
    decimal Amount,
    string Currency = "USD") : IRequest;
