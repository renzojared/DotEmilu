using DotEmilu.Abstractions;
using DotEmilu.AspNetCore;
using DotEmilu.Samples.Domain.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace DotEmilu.Samples.FullApp.Features.ConfirmInvoice;

// Endpoint
public static partial class ConfirmInvoice
{
    internal static IEndpointRouteBuilder MapConfirmInvoice(this IEndpointRouteBuilder builder)
    {
        builder
            .MapPost("{id:int}/confirm",
                ([FromRoute] int id,
                        [FromBody] ConfirmInvoiceBody body,
                        HttpHandler<ConfirmInvoiceRequest, ConfirmInvoiceResponse> handler,
                        CancellationToken ct) =>
                    AsDelegate.ForAsync<ConfirmInvoiceRequest, ConfirmInvoiceResponse>(result =>
                        TypedResults.Ok(result))(
                        new ConfirmInvoiceRequest(id, body.ConfirmationNotes), handler, ct))
            .WithName("ConfirmInvoice")
            .WithSummary("Confirms an invoice (Orchestration Pattern)")
            .Produces<ConfirmInvoiceResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return builder;
    }

    public record ConfirmInvoiceBody(string ConfirmationNotes);
}

// Handler
public static partial class ConfirmInvoice
{
    internal sealed class Handler(
        IVerifier<ConfirmInvoiceRequest> verifier,
        IHandler<ValidateInvoiceForConfirmationRequest> subHandler,
        IVerifier<ValidateInvoiceForConfirmationRequest> subVerifier,
        IInvoiceCommands db)
        : Handler<ConfirmInvoiceRequest, ConfirmInvoiceResponse>(verifier)
    {
        private readonly IVerifier<ConfirmInvoiceRequest> _verifier = verifier;

        protected override async Task<ConfirmInvoiceResponse?> HandleUseCaseAsync(
            ConfirmInvoiceRequest request,
            CancellationToken cancellationToken)
        {
            // 1. Delegate to validation sub-handler
            var validateRequest = new ValidateInvoiceForConfirmationRequest(request.InvoiceId);
            await subHandler.HandleAsync(validateRequest, cancellationToken);

            // 2. Propagate errors if any
            if (!subVerifier.IsValid)
            {
                _verifier.AddValidationErrors(subVerifier.ValidationErrors.ToList());
                return null;
            }

            // 3. Process the actual confirmation
            var invoice = await db.Invoices.FindAsync([request.InvoiceId], cancellationToken: cancellationToken);
            if (invoice is not null)
            {
                // In a real app we might update state here, e.g. status = Confirmed
            }

            return new ConfirmInvoiceResponse(
                InvoiceId: request.InvoiceId,
                ConfirmationCode: $"CONF-{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
                ConfirmedAt: DateTimeOffset.UtcNow);
        }
    }
}
