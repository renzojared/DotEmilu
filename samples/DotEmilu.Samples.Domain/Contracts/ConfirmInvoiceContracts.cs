namespace DotEmilu.Samples.Domain.Contracts;

/// <summary>
/// Request to fully confirm an invoice, orchestrating validation and returning a typed response.
/// </summary>
public record ConfirmInvoiceRequest(
    int InvoiceId,
    string ConfirmationNotes) : IRequest<ConfirmInvoiceResponse>;

/// <summary>
/// Result of a successfully confirmed invoice.
/// </summary>
public record ConfirmInvoiceResponse(
    int InvoiceId,
    string ConfirmationCode,
    DateTimeOffset ConfirmedAt);

/// <summary>
/// Internal request to validate an invoice's business rules before it can be confirmed.
/// </summary>
public record ValidateInvoiceForConfirmationRequest(int InvoiceId) : IRequest;
