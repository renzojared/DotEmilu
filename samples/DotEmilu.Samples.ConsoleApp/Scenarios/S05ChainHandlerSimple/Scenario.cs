using DotEmilu.Samples.Domain.Contracts;

namespace DotEmilu.Samples.ConsoleApp.Scenarios.S05ChainHandlerSimple;

/// <summary>
/// S05 — Chain of Responsibility: single step.
/// <para>
/// Shows the minimal wiring for <see cref="ChainHandler{TChain}"/>:
/// <list type="number">
///   <item>
///     Resolve the concrete chain handler from DI (registered by
///     <c>AddChainHandlers(assembly)</c>).
///   </item>
///   <item>
///     Call <c>ContinueAsync</c> directly — no <c>SetSuccessor</c> needed when
///     there is only one step.
///   </item>
/// </list>
/// See S06 for the multi-step variant that chains several handlers together using
/// the <em>Processor</em> pattern.
/// </para>
/// </summary>
internal sealed class Scenario : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("05", "ChainHandler — single step (no successor)");

        await using var provider = Container.Build();

        // ── Path A: Single step — no successor wired ─────────────────────────
        Print.Step("A", "Chain with one link — Successor is null, chain ends here");

        await using (var scope = provider.CreateAsyncScope())
        {
            var logHandler = scope.ServiceProvider.GetRequiredService<LogChainHandler>();

            var request = new CreateInvoiceRequest(
                Number: "INV-005",
                Description: "Design services",
                Amount: 2_500.00m,
                Date: DateOnly.FromDateTime(DateTime.Today));

            await logHandler.ContinueAsync(request, CancellationToken.None);
            Console.WriteLine("  ✅ Single-step chain completed.");
        }

        // ── Path B: Two steps wired via SetSuccessor ─────────────────────────
        // Chain handlers have no constructor dependencies, so we instantiate them
        // directly.  In real apps each link would be resolved from DI — but since
        // they are scoped, two *different* concrete types are needed per scope.
        // S06 shows that proper pattern with distinct handler classes.
        Print.Step("B", "Two links wired manually — SetSuccessor connects them");

        var firstLink = new LogChainHandler();
        var secondLink = new LogChainHandler();
        firstLink.SetSuccessor(secondLink);

        var chainRequest = new CreateInvoiceRequest(
            Number: "INV-006",
            Description: "Hosting setup",
            Amount: 750.00m,
            Date: DateOnly.FromDateTime(DateTime.Today));

        await firstLink.ContinueAsync(chainRequest, CancellationToken.None);
        Console.WriteLine("  ✅ Two-step chain completed.");
    }
}
