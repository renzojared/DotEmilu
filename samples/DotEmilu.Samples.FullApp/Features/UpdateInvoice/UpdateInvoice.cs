using DotEmilu.Abstractions;
using DotEmilu.AspNetCore;
using DotEmilu.Samples.Domain.Contracts;
using DotEmilu.Samples.FullApp.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.FullApp.Features.UpdateInvoice;

public static class UpdateInvoice
{
    internal static IEndpointRouteBuilder MapUpdateInvoice(this IEndpointRouteBuilder builder)
    {
        builder
            .MapPut("{id:int}",
                (int id, UpdateInvoiceRequest request, HttpHandler<UpdateInvoiceRequest> handler,
                        CancellationToken ct) =>
                    AsDelegate.ForAsync<UpdateInvoiceRequest>(TypedResults.NoContent)(
                        request with { Id = id }, handler, ct))
            .WithName("UpdateInvoice")
            .WithSummary("Updates an existing invoice")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return builder;
    }

    internal sealed class Handler(IVerifier<UpdateInvoiceRequest> verifier, InvoiceDbContext db)
        : Handler<UpdateInvoiceRequest>(verifier)
    {
        private readonly IVerifier<UpdateInvoiceRequest> _verifier = verifier;

        protected override async Task HandleUseCaseAsync(
            UpdateInvoiceRequest request, CancellationToken cancellationToken)
        {
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

            if (invoice is null)
            {
                _verifier.AddValidationError("Id", $"Invoice with Id {request.Id} was not found.");
                return;
            }

            invoice.Number = request.Number;
            invoice.Description = request.Description;
            invoice.Amount = request.Amount;
            invoice.Date = request.Date;
            invoice.IsPaid = request.IsPaid;

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
