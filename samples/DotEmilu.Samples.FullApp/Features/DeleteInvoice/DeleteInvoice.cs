using DotEmilu.Abstractions;
using DotEmilu.AspNetCore;
using DotEmilu.Samples.Domain.Contracts;
using DotEmilu.Samples.FullApp.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.FullApp.Features.DeleteInvoice;

// Endpoint
public static partial class DeleteInvoice
{
    internal static IEndpointRouteBuilder MapDeleteInvoice(this IEndpointRouteBuilder builder)
    {
        builder
            .MapDelete("{id:int}",
                ([FromRoute] int id, HttpHandler<DeleteInvoiceRequest> handler,
                        CancellationToken ct) =>
                    AsDelegate.ForAsync<DeleteInvoiceRequest>(TypedResults.NoContent)(
                        new DeleteInvoiceRequest(id), handler, ct))
            .WithName("DeleteInvoice")
            .WithSummary("Soft-deletes an invoice")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return builder;
    }
}

// Handler
public static partial class DeleteInvoice
{
    internal sealed class Handler(IVerifier<DeleteInvoiceRequest> verifier, InvoiceDbContext db)
        : Handler<DeleteInvoiceRequest>(verifier)
    {
        private readonly IVerifier<DeleteInvoiceRequest> _verifier = verifier;

        protected override async Task HandleUseCaseAsync(DeleteInvoiceRequest request,
            CancellationToken cancellationToken)
        {
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

            if (invoice is null)
            {
                _verifier.AddValidationError("Id", $"Invoice with Id {request.Id} was not found.");
                return;
            }

            db.Invoices.Remove(invoice);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
