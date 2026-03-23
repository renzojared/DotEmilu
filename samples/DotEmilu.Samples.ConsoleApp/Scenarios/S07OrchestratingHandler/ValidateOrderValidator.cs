namespace DotEmilu.Samples.ConsoleApp.Scenarios.S07OrchestratingHandler;

/// <summary>
/// Validates the structure of a <see cref="ValidateOrderRequest"/> using FluentValidation rules.
/// <para>
/// Structural rules — non-empty identifiers, positive amounts — are expressed here.
/// The runtime business rule (approval threshold) lives inside
/// <see cref="ValidateOrderHandler"/> and is evaluated via
/// <c>_verifier.AddValidationError()</c>.
/// </para>
/// </summary>
internal sealed class ValidateOrderValidator : AbstractValidator<ValidateOrderRequest>
{
    public ValidateOrderValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.")
            .MaximumLength(50).WithMessage("Order ID cannot exceed 50 characters.");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Order amount must be greater than zero.");
    }
}
