namespace DotEmilu.Samples.Domain.Contracts;

/// <summary>
/// Request to retrieve a single invoice by its identifier.
/// Demonstrates the null → 404 pattern using a custom
/// <c>AsDelegate.ForAsync&lt;TRequest, TResponse&gt;</c> result function.
/// </summary>
public record GetInvoiceByIdRequest(int Id) : IRequest<InvoiceResponse>;

/// <summary>
/// Read-only projection of an invoice, used as an HTTP response payload.
/// </summary>
public record InvoiceResponse(
    int Id,
    string Number,
    string Description,
    decimal Amount,
    DateOnly Date,
    bool IsPaid);
