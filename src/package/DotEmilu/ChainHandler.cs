namespace DotEmilu;

public abstract class ChainHandler<TChain>
{
    protected ChainHandler<TChain>? Successor;

    public ChainHandler<TChain> SetSuccessor(ChainHandler<TChain> successor)
        => Successor = successor;

    public abstract Task ContinueAsync(TChain chain, CancellationToken cancellationToken = default);
}