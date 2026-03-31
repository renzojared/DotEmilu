namespace DotEmilu.Samples.ConsoleApp.Scenarios.S06ChainHandlerMultiStep;

/// <summary>
/// Third and final link in the multi-step sync chain.
/// "Persists" every enriched payload from <see cref="SyncJobContext.EnrichedPayloads"/>
/// (simulated here with a console write) and marks
/// <see cref="SyncJobContext.IsPersisted"/> as <see langword="true"/> once done.
/// <para>
/// In a real application this step would write to a database, publish to a message
/// broker, or call a remote API.  The chain terminates here — no
/// <c>Successor</c> is set, so the implicit guard in
/// <see cref="ChainHandler{TChain}.Successor"/> naturally ends the pipeline.
/// </para>
/// </summary>
internal sealed class PersistJobHandler : ChainHandler<SyncJobContext>
{
    public override async Task ContinueAsync(SyncJobContext chain, CancellationToken cancellationToken)
    {
        Console.WriteLine("  💾 [PersistJobHandler] Persisting enriched payloads…");

        foreach (var payload in chain.EnrichedPayloads)
        {
            // Simulate an async I/O write (e.g. DbContext.SaveChangesAsync).
            await Task.Delay(1, cancellationToken);
            Console.WriteLine($"     ✓ Persisted: {payload[..Math.Min(80, payload.Length)]}…");
        }

        chain.IsPersisted = true;

        Console.WriteLine($"  ✅ [PersistJobHandler] {chain.EnrichedPayloads.Count} payload(s) committed.");

        // No successor — this is the terminal link of the chain.
        if (Successor is not null)
            await Successor.ContinueAsync(chain, cancellationToken);
    }
}
