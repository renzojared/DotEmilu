using DotEmilu.Abstractions;
using DotEmilu.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace DotEmilu.Samples.FullApp.Features.SyncInvoices;

public record SyncInvoicesRequest(int[] InvoiceIds) : IRequest<SyncInvoicesResponse>;

public record SyncInvoicesResponse(
    string JobId,
    int SyncedCount,
    bool IsPersisted);

// Endpoint
public static partial class SyncInvoices
{
    internal static IEndpointRouteBuilder MapSyncInvoices(this IEndpointRouteBuilder builder)
    {
        builder
            .MapPost("sync",
                ([FromBody] SyncInvoicesRequest request,
                        HttpHandler<SyncInvoicesRequest, SyncInvoicesResponse> handler,
                        CancellationToken ct) =>
                    AsDelegate.ForAsync<SyncInvoicesRequest, SyncInvoicesResponse>(TypedResults.Ok)(
                        request, handler, ct))
            .WithName("SyncInvoices")
            .WithSummary("Synchronizes multiple invoices using a pipeline (ChainHandler Pattern)")
            .Produces<SyncInvoicesResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return builder;
    }
}

// Handler
public static partial class SyncInvoices
{
    internal sealed class Handler(
        IVerifier<SyncInvoicesRequest> verifier,
        IHandler<SyncInvoicesContext> processor)
        : Handler<SyncInvoicesRequest, SyncInvoicesResponse>(verifier)
    {
        private readonly IVerifier<SyncInvoicesRequest> _verifier = verifier;

        protected override async Task<SyncInvoicesResponse?> HandleUseCaseAsync(
            SyncInvoicesRequest request,
            CancellationToken cancellationToken)
        {
            var context = new SyncInvoicesContext
            {
                SyncJobId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                TargetInvoiceIds = request.InvoiceIds
            };

            await processor.HandleAsync(context, cancellationToken);

            if (context.ValidationErrors.Count <= 0)
                return new SyncInvoicesResponse(
                    context.SyncJobId,
                    context.SyncedCount,
                    context.IsPersisted);

            foreach (var err in context.ValidationErrors)
            {
                _verifier.AddValidationError("SyncJob", err);
            }

            return null;

        }
    }
}
