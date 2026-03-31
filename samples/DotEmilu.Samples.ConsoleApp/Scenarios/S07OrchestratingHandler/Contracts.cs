namespace DotEmilu.Samples.ConsoleApp.Scenarios.S07OrchestratingHandler;

/// <summary>
/// Request to validate an order before processing.
/// Handled by the inner <c>ValidateOrderHandler</c> — resolved as a sub-handler
/// by the outer <c>ProcessOrderHandler</c>.
/// </summary>
public sealed record ValidateOrderRequest(string OrderId, decimal TotalAmount) : IRequest;

/// <summary>
/// Request to fully process an order (validate + enrich + confirm).
/// The outer <c>ProcessOrderHandler</c> orchestrates <c>ValidateOrderHandler</c>
/// internally and propagates any validation errors up to its own verifier.
/// </summary>
public sealed record ProcessOrderRequest(
    string OrderId,
    string CustomerEmail,
    decimal TotalAmount) : IRequest<ProcessOrderResult>;

/// <summary>
/// Result of a successfully processed order.
/// </summary>
public sealed record ProcessOrderResult(
    string OrderId,
    string ConfirmationCode,
    DateTimeOffset ProcessedAt);
