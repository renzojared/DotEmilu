namespace DotEmilu.Samples.Domain.Contracts;

/// <summary>
/// Request to create an invoice and return a confirmation message.
/// Demonstrates <see cref="IRequest{TResponse}"/> with a string response.
/// </summary>
public record CreateInvoiceWithConfirmationRequest(
    string Number,
    string Description,
    decimal Amount,
    DateOnly Date) : IRequest<string>;
