using DotEmilu.Samples.Domain.Contracts;
using FluentValidation;

namespace DotEmilu.Samples.FullApp.Features.CreateInvoice;

internal sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Invoice number is required.")
            .MaximumLength(20).WithMessage("Invoice number cannot exceed 20 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Date cannot be in the future.");
    }
}
