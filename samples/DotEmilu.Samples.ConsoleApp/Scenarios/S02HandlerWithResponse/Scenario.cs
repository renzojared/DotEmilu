using DotEmilu.Samples.Domain.Contracts;

namespace DotEmilu.Samples.ConsoleApp.Scenarios.S02HandlerWithResponse;

/// <summary>
/// S02 — Handler with typed response (<see cref="Handler{TRequest,TResponse}"/>).
/// <para>
/// Shows how to resolve and call <c>IHandler&lt;TRequest, TResponse&gt;</c> and inspect
/// the returned value. Runs two paths:
/// <list type="number">
///   <item>Validation fails → handler returns <c>null</c>, verifier carries the errors.</item>
///   <item>Valid request → handler returns the typed confirmation string.</item>
/// </list>
/// </para>
/// </summary>
internal sealed class Scenario : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("02", "Handler with typed response (Handler<TRequest, TResponse>)");

        await using var provider = Container.Build();

        // ── Path A: INVALID request ─────────────────────────────────────────
        Print.Step("A", "INVALID request — handler returns null, verifier holds errors");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider
                .GetRequiredService<IHandler<CreateInvoiceWithConfirmationRequest, string>>();
            var verifier = scope.ServiceProvider
                .GetRequiredService<IVerifier<CreateInvoiceWithConfirmationRequest>>();

            var invalid = new CreateInvoiceWithConfirmationRequest(
                Number: "",
                Description: "",
                Amount: -1m,
                Date: DateOnly.FromDateTime(DateTime.Today.AddDays(5)));

            var result = await handler.HandleAsync(invalid, CancellationToken.None);

            Console.WriteLine($"  📦 Result: {result ?? "(null — validation blocked execution)"}");

            if (!verifier.IsValid)
            {
                Console.WriteLine("  ❌ Validation errors:");
                foreach (var error in verifier.ValidationErrors)
                    Console.WriteLine($"     • {error.PropertyName}: {error.ErrorMessage}");
            }
        }

        // ── Path B: VALID request ────────────────────────────────────────────
        Print.Step("B", "VALID request — handler executes and returns typed value");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider
                .GetRequiredService<IHandler<CreateInvoiceWithConfirmationRequest, string>>();

            var valid = new CreateInvoiceWithConfirmationRequest(
                Number: "INV-002",
                Description: "Consulting services",
                Amount: 3_000.00m,
                Date: DateOnly.FromDateTime(DateTime.Today));

            var result = await handler.HandleAsync(valid, CancellationToken.None);

            Console.WriteLine($"  📨 Response: {result}");
        }
    }
}
