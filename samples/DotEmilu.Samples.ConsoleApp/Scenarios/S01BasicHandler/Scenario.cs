using DotEmilu.Samples.Domain.Contracts;

namespace DotEmilu.Samples.ConsoleApp.Scenarios.S01BasicHandler;

/// <summary>
/// S01 — Basic Handler + Verifier.
/// <para>
/// Demonstrates the two fundamental paths of a <see cref="Handler{TRequest}"/>:
/// <list type="number">
///   <item>FluentValidation rejects the request → <c>verifier.IsValid == false</c>.</item>
///   <item>A valid request passes validation and the use-case logic executes.</item>
/// </list>
/// Each run builds its own <see cref="IServiceProvider"/> so there is zero shared
/// state with any other scenario.
/// </para>
/// </summary>
internal sealed class Scenario : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("01", "Handler + Verifier (invalid → valid)");

        await using var provider = Container.Build();

        // ── Path A: INVALID request ─────────────────────────────────────────
        Print.Step("A", "INVALID request — FluentValidation rejects it");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<IHandler<CreateInvoiceRequest>>();
            var verifier = scope.ServiceProvider.GetRequiredService<IVerifier<CreateInvoiceRequest>>();

            var invalid = new CreateInvoiceRequest(
                Number: "",
                Description: "",
                Amount: -5m,
                Date: DateOnly.FromDateTime(DateTime.Today.AddDays(10)));

            await handler.HandleAsync(invalid, CancellationToken.None);

            if (!verifier.IsValid)
            {
                Console.WriteLine("  ❌ Validation errors:");
                foreach (var error in verifier.ValidationErrors)
                    Console.WriteLine($"     • {error.PropertyName}: {error.ErrorMessage}");
            }
        }

        // ── Path B: VALID request ────────────────────────────────────────────
        Print.Step("B", "VALID request — use-case executes");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<IHandler<CreateInvoiceRequest>>();

            var valid = new CreateInvoiceRequest(
                Number: "INV-001",
                Description: "Web development services",
                Amount: 1_500.00m,
                Date: DateOnly.FromDateTime(DateTime.Today));

            await handler.HandleAsync(valid, CancellationToken.None);
        }
    }
}
