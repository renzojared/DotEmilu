namespace DotEmilu.Samples.ConsoleApp.Scenarios.S06ChainHandlerMultiStep;

/// <summary>
/// First link in the multi-step sync chain.
/// Validates that each data source in <see cref="SyncJobContext.DataSources"/> is
/// non-empty and does not contain spaces, populating:
/// <list type="bullet">
///   <item><see cref="SyncJobContext.ValidatedItems"/> with accepted source names.</item>
///   <item><see cref="SyncJobContext.ValidationErrors"/> with rejection reasons.</item>
/// </list>
/// After validation the chain continues to <c>EnrichJobHandler</c> only when at least
/// one source passed.  An empty validated set short-circuits the chain intentionally.
/// </summary>
internal sealed class ValidateJobHandler : ChainHandler<SyncJobContext>
{
    public override async Task ContinueAsync(SyncJobContext chain, CancellationToken cancellationToken)
    {
        Console.WriteLine("  🔍 [ValidateJobHandler] Validating data sources…");

        foreach (var source in chain.DataSources)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                chain.ValidationErrors.Add("Rejected empty/whitespace data source.");
                Console.WriteLine("     ✗ Rejected: (empty)");
            }
            else if (source.Contains(' '))
            {
                chain.ValidationErrors.Add($"Rejected '{source}': source names must not contain spaces.");
                Console.WriteLine($"     ✗ Rejected: '{source}' — contains spaces");
            }
            else
            {
                chain.ValidatedItems.Add(source);
                Console.WriteLine($"     ✓ Accepted: '{source}'");
            }
        }

        Console.WriteLine(
            $"  📊 [ValidateJobHandler] {chain.ValidatedItems.Count}/{chain.DataSources.Length} sources accepted.");

        if (chain.ValidatedItems.Count == 0)
        {
            Console.WriteLine("  ⛔ [ValidateJobHandler] No valid sources — short-circuiting chain.");
            return; // intentional: do NOT call Successor when nothing to process
        }

        if (Successor is not null)
            await Successor.ContinueAsync(chain, cancellationToken);
    }
}
