namespace DotEmilu.Samples.ConsoleApp.Scenarios.S07OrchestratingHandler;

/// <summary>
/// S07 — Orchestrating Handler (handler that composes a sub-handler).
/// <para>
/// Demonstrates how an outer <see cref="ProcessOrderHandler"/> delegates part of its
/// logic to an inner <see cref="ValidateOrderHandler"/> and propagates any errors the
/// sub-handler produced back to the caller through its own verifier.
/// </para>
/// <para>
/// Two execution paths are covered:
/// <list type="number">
///   <item>
///     <b>Success</b> — the order passes structural validation (FluentValidation) and
///     the business-rule check inside the sub-handler.  The outer handler produces a
///     <see cref="ProcessOrderResult"/> with a confirmation code.
///   </item>
///   <item>
///     <b>Sub-handler failure</b> — the order amount exceeds the approval threshold.
///     <see cref="ValidateOrderHandler"/> adds the error via
///     <c>_verifier.AddValidationError()</c>; the outer handler detects
///     <c>subVerifier.IsValid == false</c>, copies the errors into its own verifier,
///     and returns <c>null</c>.  The caller inspects the outer verifier to retrieve the
///     consolidated error list.
///   </item>
/// </list>
/// </para>
/// </summary>
internal sealed class Scenario : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("07", "Orchestrating Handler (outer handler composes a sub-handler)");

        await using var provider = Container.Build();

        // ── Path A: SUCCESS ──────────────────────────────────────────────────
        // Order amount is below the approval threshold → both handlers succeed.
        Print.Step("A", "Valid order — sub-handler passes, outer handler returns result");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider
                .GetRequiredService<IHandler<ProcessOrderRequest, ProcessOrderResult>>();
            var verifier = scope.ServiceProvider
                .GetRequiredService<IVerifier<ProcessOrderRequest>>();

            var request = new ProcessOrderRequest(
                OrderId: "ORD-100",
                CustomerEmail: "customer@example.com",
                TotalAmount: 4_500.00m);

            var result = await handler.HandleAsync(request, CancellationToken.None);

            if (verifier.IsValid && result is not null)
            {
                Console.WriteLine($"  📦 Confirmation : {result.ConfirmationCode}");
                Console.WriteLine($"  📦 Processed at : {result.ProcessedAt:u}");
            }
        }

        // ── Path B: SUB-HANDLER FAILURE ──────────────────────────────────────
        // Order amount exceeds the approval threshold → ValidateOrderHandler adds a
        // runtime error via _verifier.AddValidationError(); the outer handler copies
        // those errors into its own verifier and returns null.
        Print.Step("B", "Amount exceeds threshold — sub-handler fails, errors propagate to caller");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider
                .GetRequiredService<IHandler<ProcessOrderRequest, ProcessOrderResult>>();
            var verifier = scope.ServiceProvider
                .GetRequiredService<IVerifier<ProcessOrderRequest>>();

            var overThreshold = new ProcessOrderRequest(
                OrderId: "ORD-101",
                CustomerEmail: "vip@example.com",
                TotalAmount: 25_000.00m);

            var result = await handler.HandleAsync(overThreshold, CancellationToken.None);

            Console.WriteLine($"  📦 Result: {result ?? (object)"(null — sub-handler blocked processing)"}");

            if (!verifier.IsValid)
            {
                Console.WriteLine("  ❌ Propagated validation errors:");
                foreach (var error in verifier.ValidationErrors)
                    Console.WriteLine($"     • {error.PropertyName}: {error.ErrorMessage}");
            }
        }

        // ── Path C: STRUCTURAL FAILURE (outer FluentValidation) ──────────────
        // The outer handler's own validator rejects the request before the use-case
        // body even runs — the sub-handler is never called.
        Print.Step("C", "Empty OrderId — outer FluentValidation rejects before sub-handler runs");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider
                .GetRequiredService<IHandler<ProcessOrderRequest, ProcessOrderResult>>();
            var verifier = scope.ServiceProvider
                .GetRequiredService<IVerifier<ProcessOrderRequest>>();

            var invalid = new ProcessOrderRequest(
                OrderId: "",
                CustomerEmail: "someone@example.com",
                TotalAmount: 500.00m);

            var result = await handler.HandleAsync(invalid, CancellationToken.None);

            Console.WriteLine($"  📦 Result: {result ?? (object)"(null — outer validation blocked execution)"}");

            if (!verifier.IsValid)
            {
                Console.WriteLine("  ❌ Outer FluentValidation errors:");
                foreach (var error in verifier.ValidationErrors)
                    Console.WriteLine($"     • {error.PropertyName}: {error.ErrorMessage}");
            }
        }
    }
}
