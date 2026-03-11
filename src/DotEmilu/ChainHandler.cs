namespace DotEmilu;

public abstract class ChainHandler<TChain>
{
    protected ChainHandler<TChain>? Successor { get; private set; }

    public ChainHandler<TChain> SetSuccessor(ChainHandler<TChain> successor)
        => Successor = successor;

    public abstract Task ContinueAsync(TChain chain, CancellationToken cancellationToken);
}