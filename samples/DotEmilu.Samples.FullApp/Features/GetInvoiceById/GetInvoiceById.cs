using DotEmilu.Abstractions;
using DotEmilu.AspNetCore;
using DotEmilu.Samples.Domain.Contracts;
using DotEmilu.Samples.FullApp.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.FullApp.Features.GetInvoiceById;

public static class GetInvoiceById
{
    internal static IEndpointRouteBuilder MapGetInvoiceById(this IEndpointRouteBuilder builder)
    {
        builder
            .MapGet("{id:int}",
                ([FromRoute] int id,
                        HttpHandler<GetInvoiceByIdRequest, InvoiceResponse> handler,
                        CancellationToken ct) =>
                    AsDelegate.ForAsync<GetInvoiceByIdRequest, InvoiceResponse>(result =>
                        result is null ? TypedResults.NotFound() : TypedResults.Ok(result))(
                        new GetInvoiceByIdRequest(id), handler, ct))
            .WithName("GetInvoiceById")
            .WithSummary("Gets an invoice by its ID")
            .Produces<InvoiceResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return builder;
    }

    internal sealed class Handler(IVerifier<GetInvoiceByIdRequest> verifier, InvoiceDbContext db)
        : Handler<GetInvoiceByIdRequest, InvoiceResponse>(verifier)
    {
        protected override async Task<InvoiceResponse?> HandleUseCaseAsync(
            GetInvoiceByIdRequest request, CancellationToken cancellationToken)
        {
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

            if (invoice is null)
                return null;

            return new InvoiceResponse(
                invoice.Id,
                invoice.Number,
                invoice.Description,
                invoice.Amount,
                invoice.Date,
                invoice.IsPaid);
        }
    }
}
