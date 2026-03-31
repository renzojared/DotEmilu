using DotEmilu.Samples.Domain.Contracts;

namespace DotEmilu.Samples.ConsoleApp.Scenarios.S02HandlerWithResponse;

/// <summary>
/// Validates a <see cref="CreateInvoiceWithConfirmationRequest"/> using FluentValidation rules.
/// Registered automatically via <c>AddValidatorsFromAssembly</c> in <see cref="Container"/>.
/// Without this validator the <see cref="IVerifier{TRequest}"/> would find no rules and
/// would always report <c>IsValid = true</c>, making Path A of the scenario meaningless.
/// </summary>
internal sealed class CreateInvoiceWithConfirmationValidator
    : AbstractValidator<CreateInvoiceWithConfirmationRequest>
{
    public CreateInvoiceWithConfirmationValidator()
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
