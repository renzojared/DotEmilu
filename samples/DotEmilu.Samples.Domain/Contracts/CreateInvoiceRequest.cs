namespace DotEmilu.Samples.Domain.Contracts;

/// <summary>
/// Request to create a new invoice.
/// </summary>
public record CreateInvoiceRequest(
    string Number,
    string Description,
    decimal Amount,
    DateOnly Date) : IRequest;
