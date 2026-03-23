using DotEmilu.Abstractions;
using DotEmilu.AspNetCore;
using DotEmilu.EntityFrameworkCore.Extensions;
using DotEmilu.Samples.Domain.Contracts;
using DotEmilu.Samples.Domain.Entities;
using DotEmilu.Samples.FullApp.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DotEmilu.Samples.FullApp.Features.GetInvoices;

public static class GetInvoices
{
    internal static IEndpointRouteBuilder MapGetInvoices(this IEndpointRouteBuilder builder)
    {
        builder
            .MapGet(string.Empty,
                ([AsParameters] GetInvoicesRequest request,
                        HttpHandler<GetInvoicesRequest, PaginatedList<Invoice>> handler,
                        CancellationToken ct) =>
                    AsDelegate.ForAsync<GetInvoicesRequest, PaginatedList<Invoice>>()(request, handler, ct))
            .WithName("GetInvoices")
            .WithSummary("Gets a paginated list of invoices")
            .Produces<PaginatedList<Invoice>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return builder;
    }

    internal sealed class Handler(IVerifier<GetInvoicesRequest> verifier, InvoiceDbContext db)
        : Handler<GetInvoicesRequest, PaginatedList<Invoice>>(verifier)
    {
        protected override async Task<PaginatedList<Invoice>?> HandleUseCaseAsync(
            GetInvoicesRequest request, CancellationToken cancellationToken)
            => await db.Invoices
                .OrderByDescending(i => i.Created)
                .AsPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}
