using DotEmilu.Samples.Domain.Contracts;

namespace DotEmilu.Samples.ConsoleApp.Scenarios.S02HandlerWithResponse;

/// <summary>
/// Handles invoice creation and returns a typed confirmation string.
/// Demonstrates <see cref="Handler{TRequest,TResponse}"/> — the variant that produces a value.
/// </summary>
internal sealed class ConfirmInvoiceHandler(IVerifier<CreateInvoiceWithConfirmationRequest> verifier)
    : Handler<CreateInvoiceWithConfirmationRequest, string>(verifier)
{
    protected override Task<string?> HandleUseCaseAsync(
        CreateInvoiceWithConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"  ✅ Invoice #{request.Number} confirmed — ${request.Amount}");
        var confirmation = $"Invoice {request.Number} for ${request.Amount} created successfully.";
        return Task.FromResult<string?>(confirmation);
    }
}
