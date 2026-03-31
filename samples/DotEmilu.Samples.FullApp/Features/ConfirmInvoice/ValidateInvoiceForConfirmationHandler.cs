using DotEmilu.Abstractions;
using DotEmilu.Samples.Domain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.FullApp.Features.ConfirmInvoice;

/// <summary>
/// Sub-handler that validates an invoice's business rules before it can be confirmed.
/// This checks runtime rules not expressible in static FluentValidation rules.
/// </summary>
internal sealed class ValidateInvoiceForConfirmationHandler(
    IVerifier<ValidateInvoiceForConfirmationRequest> verifier,
    IInvoiceQueries db)
    : Handler<ValidateInvoiceForConfirmationRequest>(verifier)
{
    private readonly IVerifier<ValidateInvoiceForConfirmationRequest> _verifier = verifier;

    protected override async Task HandleUseCaseAsync(
        ValidateInvoiceForConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            _verifier.AddValidationError("InvoiceId", $"Invoice with Id {request.InvoiceId} was not found.");
            return;
        }

        if (invoice.IsPaid)
        {
            _verifier.AddValidationError("InvoiceId",
                $"Invoice {request.InvoiceId} is already paid and cannot be confirmed again.");
        }
    }
}
