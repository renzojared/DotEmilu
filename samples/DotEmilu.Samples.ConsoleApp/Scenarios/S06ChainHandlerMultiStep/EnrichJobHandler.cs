namespace DotEmilu.Samples.ConsoleApp.Scenarios.S06ChainHandlerMultiStep;

/// <summary>
/// Second link in the multi-step sync chain.
/// Transforms each validated data source name from <see cref="SyncJobContext.ValidatedItems"/>
/// into an enriched payload (simulated here as a timestamped JSON-like string), populating
/// <see cref="SyncJobContext.EnrichedPayloads"/>.
/// <para>
/// In a real application this step would call external services, resolve metadata,
/// or hydrate the context with additional data before the persist step runs.
/// </para>
/// </summary>
internal sealed class EnrichJobHandler : ChainHandler<SyncJobContext>
{
    public override async Task ContinueAsync(SyncJobContext chain, CancellationToken cancellationToken)
    {
        Console.WriteLine("  🔧 [EnrichJobHandler] Enriching validated items…");

        foreach (var source in chain.ValidatedItems)
        {
            var payload =
                $"{{ \"source\": \"{source}\", \"enrichedAt\": \"{DateTimeOffset.UtcNow:O}\", \"recordCount\": {Random.Shared.Next(10, 500)} }}";
            chain.EnrichedPayloads.Add(payload);
            Console.WriteLine($"     ✓ Enriched: '{source}'");
        }

        Console.WriteLine($"  📦 [EnrichJobHandler] {chain.EnrichedPayloads.Count} payload(s) ready.");

        if (Successor is not null)
            await Successor.ContinueAsync(chain, cancellationToken);
    }
}
