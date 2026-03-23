using DotEmilu.Samples.Domain.Contracts;
using FluentValidation;

namespace DotEmilu.Samples.FullApp.Features.GetInvoices;

internal sealed class GetInvoicesValidator : AbstractValidator<GetInvoicesRequest>
{
    public GetInvoicesValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than zero.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
