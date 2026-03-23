namespace DotEmilu.Samples.ConsoleApp.Scenarios.S06ChainHandlerMultiStep;

/// <summary>
/// S06 — Chain of Responsibility: multi-step Processor pattern.
/// <para>
/// Demonstrates the canonical DotEmilu approach for complex pipelines:
/// <list type="number">
///   <item>
///     A <see cref="SyncJobProcessor"/> is registered as
///     <c>IHandler&lt;SyncJobContext&gt;</c> and wires three
///     <see cref="ChainHandler{TChain}"/> links together in its constructor via
///     <c>a.SetSuccessor(b).SetSuccessor(c)</c>.
///   </item>
///   <item>
///     Callers only know about <c>IHandler&lt;SyncJobContext&gt;</c> — the
///     individual chain steps are completely hidden behind the processor.
///   </item>
/// </list>
/// Two execution paths are shown:
/// <list type="number">
///   <item>
///     <b>Full pipeline</b> — all three steps run: Validate → Enrich → Persist.
///   </item>
///   <item>
///     <b>Short-circuit</b> — the Validate step rejects all sources and explicitly
///     does <em>not</em> forward to the next link, so Enrich and Persist never run.
///   </item>
/// </list>
/// </para>
/// </summary>
internal sealed class Scenario : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("06", "ChainHandler — multi-step Processor (Validate → Enrich → Persist)");

        await using var provider = Container.Build();

        // ── Path A: FULL pipeline ────────────────────────────────────────────
        // All data sources are valid → all three chain steps execute in order.
        Print.Step("A", "Full pipeline — all sources valid, all three steps run");

        await using (var scope = provider.CreateAsyncScope())
        {
            var processor = scope.ServiceProvider
                .GetRequiredService<IHandler<SyncJobContext>>();

            var context = new SyncJobContext
            {
                JobId = "JOB-001",
                DataSources = ["orders-api", "inventory-db", "shipping-feed"],
            };

            await processor.HandleAsync(context, CancellationToken.None);

            Console.WriteLine($"  📊 ValidatedItems  : {context.ValidatedItems.Count}");
            Console.WriteLine($"  📊 EnrichedPayloads: {context.EnrichedPayloads.Count}");
            Console.WriteLine($"  📊 IsPersisted     : {context.IsPersisted}");
        }

        // ── Path B: SHORT-CIRCUIT ────────────────────────────────────────────
        // All data sources are invalid → ValidateJobHandler short-circuits the
        // chain; EnrichJobHandler and PersistJobHandler never execute.
        Print.Step("B", "Short-circuit — all sources invalid, chain stops at Validate step");

        await using (var scope = provider.CreateAsyncScope())
        {
            var processor = scope.ServiceProvider
                .GetRequiredService<IHandler<SyncJobContext>>();

            var context = new SyncJobContext
            {
                JobId = "JOB-002",
                DataSources = ["", "bad source name", "  "],
            };

            await processor.HandleAsync(context, CancellationToken.None);

            Console.WriteLine($"  📊 ValidatedItems  : {context.ValidatedItems.Count}");
            Console.WriteLine($"  📊 EnrichedPayloads: {context.EnrichedPayloads.Count}");
            Console.WriteLine($"  📊 IsPersisted     : {context.IsPersisted}");
        }

        // ── Path C: PARTIAL pipeline ─────────────────────────────────────────
        // Mix of valid and invalid sources → Validate accepts some, rejects some;
        // pipeline continues with only the accepted items.
        Print.Step("C", "Partial — mixed sources: some accepted, some rejected, pipeline continues");

        await using (var scope = provider.CreateAsyncScope())
        {
            var processor = scope.ServiceProvider
                .GetRequiredService<IHandler<SyncJobContext>>();

            var context = new SyncJobContext
            {
                JobId = "JOB-003",
                DataSources = ["payments-api", "bad name here", "analytics-db"],
            };

            await processor.HandleAsync(context, CancellationToken.None);

            Console.WriteLine($"  📊 ValidatedItems  : {context.ValidatedItems.Count}");
            Console.WriteLine($"  📊 EnrichedPayloads: {context.EnrichedPayloads.Count}");
            Console.WriteLine($"  📊 IsPersisted     : {context.IsPersisted}");
            Console.WriteLine($"  📊 ValidationErrors: {context.ValidationErrors.Count}");
        }
    }
}
