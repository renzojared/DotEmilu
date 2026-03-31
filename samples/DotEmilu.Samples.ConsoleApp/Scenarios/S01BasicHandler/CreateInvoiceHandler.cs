using DotEmilu.Samples.Domain.Contracts;

namespace DotEmilu.Samples.ConsoleApp.Scenarios.S01BasicHandler;

/// <summary>
/// Handles invoice creation without returning a value.
/// Demonstrates the simplest form of <see cref="Handler{TRequest}"/>.
/// </summary>
internal sealed class CreateInvoiceHandler(IVerifier<CreateInvoiceRequest> verifier)
    : Handler<CreateInvoiceRequest>(verifier)
{
    protected override Task HandleUseCaseAsync(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine(
            $"  ✅ Invoice created: #{request.Number} | {request.Description} | ${request.Amount} | {request.Date}");
        return Task.CompletedTask;
    }
}
