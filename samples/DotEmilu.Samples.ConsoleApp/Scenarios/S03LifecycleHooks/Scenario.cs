namespace DotEmilu.Samples.ConsoleApp.Scenarios.S03LifecycleHooks;

/// <summary>
/// S03 — Lifecycle Hooks (<c>HandleExceptionAsync</c> + <c>FinalizeAsync</c>).
/// <para>
/// Demonstrates how <see cref="BaseHandler"/> wraps every execution in a
/// try/catch/finally equivalent:
/// <list type="number">
///   <item>
///     <b>Success path</b> — only <c>FinalizeAsync</c> is called after the use-case
///     completes normally.
///   </item>
///   <item>
///     <b>Exception path</b> — <c>HandleExceptionAsync</c> is called first, then
///     <c>FinalizeAsync</c>; the exception is re-thrown so the caller can decide
///     how to handle it.
///   </item>
/// </list>
/// </para>
/// </summary>
internal sealed class Scenario : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("03", "Lifecycle Hooks (HandleExceptionAsync + FinalizeAsync)");

        await using var provider = Container.Build();

        // ── Path A: SUCCESS — FinalizeAsync runs, HandleExceptionAsync does NOT ──
        Print.Step("A", "Normal execution — only FinalizeAsync is invoked");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<IHandler<ProcessPaymentRequest>>();

            var request = new ProcessPaymentRequest(
                InvoiceId: "INV-003",
                Amount: 1_200.00m,
                Currency: "USD");

            await handler.HandleAsync(request, CancellationToken.None);
        }

        // ── Path B: EXCEPTION — HandleExceptionAsync + FinalizeAsync both run ───
        Print.Step("B", "Exception path — HandleExceptionAsync fires, then FinalizeAsync");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<IHandler<ProcessPaymentRequest>>();

            var overLimitRequest = new ProcessPaymentRequest(
                InvoiceId: "INV-004",
                Amount: 99_999.99m,
                Currency: "EUR");

            try
            {
                await handler.HandleAsync(overLimitRequest, CancellationToken.None);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"  🔴 Caller caught re-thrown exception: {ex.Message}");
            }
        }
    }
}
