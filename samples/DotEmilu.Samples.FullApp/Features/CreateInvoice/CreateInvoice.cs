using DotEmilu.Abstractions;
using DotEmilu.AspNetCore;
using DotEmilu.Samples.Domain.Contracts;
using DotEmilu.Samples.Domain.Entities;

namespace DotEmilu.Samples.FullApp.Features.CreateInvoice;

// Endpoint
public static partial class CreateInvoice
{
    internal static IEndpointRouteBuilder MapCreateInvoice(this IEndpointRouteBuilder builder)
    {
        builder
            .MapPost(string.Empty, AsDelegate.ForAsync<CreateInvoiceRequest>(TypedResults.Created))
            .WithName("CreateInvoice")
            .WithSummary("Creates a new invoice")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return builder;
    }
}

// Handler
public static partial class CreateInvoice
{
    internal sealed class Handler(IVerifier<CreateInvoiceRequest> verifier, IInvoiceCommands db)
        : Handler<CreateInvoiceRequest>(verifier)
    {
        protected override async Task HandleUseCaseAsync(
            CreateInvoiceRequest request, CancellationToken cancellationToken)
        {
            var invoice = new Invoice
            {
                Number = request.Number,
                Description = request.Description,
                Amount = request.Amount,
                Date = request.Date
            };

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
