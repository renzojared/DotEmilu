namespace DotEmilu.Samples.ConsoleApp.Scenarios.S07OrchestratingHandler;

/// <summary>
/// Validates the structure of a <see cref="ProcessOrderRequest"/> using FluentValidation rules.
/// <para>
/// This validator guards the <em>outer</em> handler.  Structural rules — non-empty
/// identifiers, valid e-mail format, positive amount — are expressed here and are
/// evaluated by the <see cref="IVerifier{TRequest}"/> before
/// <c>ProcessOrderHandler.HandleUseCaseAsync</c> is ever called.
/// </para>
/// <para>
/// The approval-threshold business rule (<c>TotalAmount &gt; 10 000</c>) is <em>not</em>
/// expressed here because it belongs to the sub-handler
/// (<see cref="ValidateOrderHandler"/>) which owns that domain concern.
/// </para>
/// </summary>
internal sealed class ProcessOrderValidator : AbstractValidator<ProcessOrderRequest>
{
    public ProcessOrderValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.")
            .MaximumLength(50).WithMessage("Order ID cannot exceed 50 characters.");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Customer e-mail is required.")
            .EmailAddress().WithMessage("Customer e-mail must be a valid address.")
            .MaximumLength(200).WithMessage("Customer e-mail cannot exceed 200 characters.");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Order amount must be greater than zero.");
    }
}
