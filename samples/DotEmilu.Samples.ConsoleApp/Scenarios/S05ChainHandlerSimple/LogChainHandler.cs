using DotEmilu.Samples.Domain.Contracts;

namespace DotEmilu.Samples.ConsoleApp.Scenarios.S05ChainHandlerSimple;

/// <summary>
/// A single-step chain handler that logs the incoming request and forwards it to the next
/// link in the chain (if one exists).
/// <para>
/// Demonstrates the minimal <see cref="ChainHandler{TChain}"/> contract:
/// <list type="bullet">
///   <item>Override <see cref="ChainHandler{TChain}.ContinueAsync"/> to add behaviour.</item>
///   <item>Always call <c>Successor?.ContinueAsync</c> to keep the chain moving.</item>
///   <item>Omitting the successor call short-circuits the chain intentionally.</item>
/// </list>
/// </para>
/// </summary>
internal sealed class LogChainHandler : ChainHandler<CreateInvoiceRequest>
{
    public override async Task ContinueAsync(CreateInvoiceRequest chain, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  📋 [LogChain] Received invoice #{chain.Number} — Amount: {chain.Amount:C}");

        if (Successor is not null)
        {
            Console.WriteLine("  ➡️  [LogChain] Forwarding to next handler in chain…");
            await Successor.ContinueAsync(chain, cancellationToken);
        }
        else
        {
            Console.WriteLine("  🔚 [LogChain] No successor — end of chain.");
        }
    }
}
