using DotEmilu.Abstractions;
using DotEmilu.Samples.Domain.Entities;

namespace DotEmilu.Samples.FullApp.Features.SyncInvoices;

/// <summary>
/// Muteable context passed through the multi-step chain.
/// Note: By implementing IRequest, it allows the processor to be resolved as IHandler<SyncInvoicesContext>.
/// </summary>
public sealed class SyncInvoicesContext : IRequest
{
    public required string SyncJobId { get; init; }
    public required int[] TargetInvoiceIds { get; init; }

    public List<string> ValidationErrors { get; } = [];
    public List<Invoice> ValidatedInvoices { get; } = [];

    public int SyncedCount { get; set; }
    public bool IsPersisted { get; set; }
}
