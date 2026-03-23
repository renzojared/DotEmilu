namespace DotEmilu.Samples.ConsoleApp.Scenarios.S06ChainHandlerMultiStep;

/// <summary>
/// Mutable context object passed through every link of the multi-step chain.
/// <para>
/// Each <see cref="ChainHandler{TChain}"/> step reads from and writes to this object,
/// accumulating state as the chain progresses.  Implementing <see cref="IRequest"/>
/// allows <see cref="SyncJobProcessor"/> to be registered and resolved as an
/// <c>IHandler&lt;SyncJobContext&gt;</c>.
/// </para>
/// </summary>
public sealed class SyncJobContext : IRequest
{
    /// <summary>Unique identifier for the synchronization job.</summary>
    public required string JobId { get; init; }

    /// <summary>Data source names to be processed.</summary>
    public required string[] DataSources { get; init; }

    // ── State accumulated by each chain step ──────────────────────────────────

    /// <summary>Validation errors added by <c>ValidateJobHandler</c>.</summary>
    public List<string> ValidationErrors { get; } = [];

    /// <summary>Items successfully validated and ready for enrichment.</summary>
    public List<string> ValidatedItems { get; } = [];

    /// <summary>Enriched payloads built by <c>EnrichJobHandler</c>.</summary>
    public List<string> EnrichedPayloads { get; } = [];

    /// <summary>
    /// Set to <see langword="true"/> by <c>PersistJobHandler</c> once all enriched
    /// payloads have been committed.
    /// </summary>
    public bool IsPersisted { get; set; }
}
