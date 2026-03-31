namespace DotEmilu.UnitTests.Core;

public class ChainHandlerTests
{
    [Fact]
    public void SetSuccessor_WhenValidHandler_ThenReturnsProvidedHandlerAndStoresLink()
    {
        var first = new PropagatingChainHandler();
        var second = new PropagatingChainHandler();

        var returned = first.SetSuccessor(second);

        Assert.Same(second, returned);
        Assert.Same(second, first.Next);
    }

    [Fact]
    public void SetSuccessor_WhenCalledTwice_ThenReplacesPreviousLink()
    {
        var first = new PropagatingChainHandler();
        var second = new PropagatingChainHandler();
        var third = new PropagatingChainHandler();

        first.SetSuccessor(second);
        first.SetSuccessor(third);

        Assert.Same(third, first.Next);
    }

    [Fact]
    public void SetSuccessor_WhenChained_ThenWiresFluentPipeline()
    {
        var first = new PropagatingChainHandler();
        var second = new PropagatingChainHandler();
        var third = new PropagatingChainHandler();

        var fluentResult = first.SetSuccessor(second).SetSuccessor(third);

        Assert.Same(third, fluentResult);
        Assert.Same(second, first.Next);
        Assert.Same(third, second.Next);
    }

    [Fact]
    public async Task ContinueAsync_WhenSuccessorIsSet_ThenCallsSuccessorContinueAsync()
    {
        var first = new PropagatingChainHandler();
        var second = new PropagatingChainHandler();
        first.SetSuccessor(second);

        await first.ContinueAsync(new object(), CancellationToken.None);

        Assert.True(first.WasCalled);
        Assert.True(second.WasCalled);
    }

    [Fact]
    public async Task ContinueAsync_WhenNoSuccessor_ThenCompletesWithoutPropagating()
    {
        var first = new PropagatingChainHandler();

        await first.ContinueAsync(new object(), CancellationToken.None);

        Assert.True(first.WasCalled);
    }

    [Fact]
    public async Task ContinueAsync_WhenShortCircuited_ThenSuccessorIsNotInvoked()
    {
        var first = new ShortCircuitChainHandler();
        var second = new PropagatingChainHandler();
        first.SetSuccessor(second);

        await first.ContinueAsync(new object(), CancellationToken.None);

        Assert.True(first.WasCalled);
        Assert.False(second.WasCalled);
    }

    [Fact]
    public async Task ContinueAsync_WhenCancelled_ThenPropagatesCancellationToken()
    {
        var handler = new CaptureTokenChainHandler();
        using var cts = new CancellationTokenSource();

        await handler.ContinueAsync(new object(), cts.Token);

        Assert.Equal(cts.Token, handler.ReceivedToken);
    }

    private sealed class PropagatingChainHandler : ChainHandler<object>
    {
        public ChainHandler<object>? Next => Successor;
        public bool WasCalled { get; private set; }

        public override async Task ContinueAsync(object chain, CancellationToken cancellationToken)
        {
            WasCalled = true;
            if (Successor is not null)
                await Successor.ContinueAsync(chain, cancellationToken);
        }
    }

    private sealed class ShortCircuitChainHandler : ChainHandler<object>
    {
        public bool WasCalled { get; private set; }

        public override Task ContinueAsync(object chain, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.CompletedTask; // Intentionally does not call Successor
        }
    }

    private sealed class CaptureTokenChainHandler : ChainHandler<object>
    {
        public CancellationToken ReceivedToken { get; private set; }

        public override Task ContinueAsync(object chain, CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
