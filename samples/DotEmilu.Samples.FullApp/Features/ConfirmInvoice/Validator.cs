using DotEmilu.Samples.Domain.Contracts;
using FluentValidation;

namespace DotEmilu.Samples.FullApp.Features.ConfirmInvoice;

internal sealed class ConfirmInvoiceValidator : AbstractValidator<ConfirmInvoiceRequest>
{
    public ConfirmInvoiceValidator()
    {
        RuleFor(x => x.InvoiceId)
            .GreaterThan(0).WithMessage("Invoice ID must be greater than zero.");

        RuleFor(x => x.ConfirmationNotes)
            .NotEmpty().WithMessage("Confirmation notes are required.")
            .MaximumLength(500).WithMessage("Confirmation notes cannot exceed 500 characters.");
    }
}

internal sealed class ValidateInvoiceForConfirmationValidator : AbstractValidator<ValidateInvoiceForConfirmationRequest>
{
    public ValidateInvoiceForConfirmationValidator()
    {
        RuleFor(x => x.InvoiceId)
            .GreaterThan(0).WithMessage("Invoice ID must be greater than zero.");
    }
}
