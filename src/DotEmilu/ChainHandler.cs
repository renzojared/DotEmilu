namespace DotEmilu;

/// <summary>
/// Represents a handler in a chain of responsibility pattern.
/// </summary>
/// <typeparam name="TChain">The type of the chain context.</typeparam>
public abstract class ChainHandler<TChain>
{
    /// <summary>Gets the next handler in the chain.</summary>
    protected ChainHandler<TChain>? Successor { get; private set; }

    /// <summary>Sets the next handler in the chain.</summary>
    /// <param name="successor">The successor handler.</param>
    /// <returns>The specified successor handler.</returns>
    public ChainHandler<TChain> SetSuccessor(ChainHandler<TChain> successor)
        => Successor = successor;

    /// <summary>Continues the execution of the chain.</summary>
    /// <param name="chain">The chain context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task ContinueAsync(TChain chain, CancellationToken cancellationToken);
}