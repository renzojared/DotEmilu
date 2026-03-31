using FluentValidation;

namespace DotEmilu.Samples.FullApp.Features.SyncInvoices;

internal sealed class SyncInvoicesValidator : AbstractValidator<SyncInvoicesRequest>
{
    public SyncInvoicesValidator()
    {
        RuleFor(x => x.InvoiceIds)
            .NotEmpty().WithMessage("At least one invoice ID must be provided.");
    }
}
