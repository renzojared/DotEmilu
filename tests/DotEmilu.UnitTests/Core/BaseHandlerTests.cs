namespace DotEmilu.UnitTests.Core;

public class BaseHandlerTests
{
    [Fact]
    public async Task HandlingAsync_WhenActionSucceeds_ThenExecutesAndCallsFinalize()
    {
        var sut = new TestableHandler();

        await sut.InvokeHandlingAsync(() => Task.CompletedTask);

        Assert.True(sut.FinalizeWasCalled);
        Assert.Null(sut.ExceptionHandled);
    }

    [Fact]
    public async Task HandlingAsync_WhenActionThrows_ThenCallsHandleExceptionAndFinalizeAndRethrows()
    {
        var sut = new TestableHandler();
        var expectedException = new InvalidOperationException("boom");

        var act = () => sut.InvokeHandlingAsync(() => throw expectedException);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal("boom", ex.Message);
        Assert.Same(expectedException, sut.ExceptionHandled);
        Assert.True(sut.FinalizeWasCalled);
    }

    [Fact]
    public async Task HandlingAsync_WhenFuncSucceeds_ThenReturnsResult()
    {
        var sut = new TestableHandler();

        var result = await sut.InvokeHandlingAsync(() => Task.FromResult(42));

        Assert.Equal(42, result);
        Assert.True(sut.FinalizeWasCalled);
    }

    [Fact]
    public async Task HandlingAsync_WhenFuncThrows_ThenCallsHandleExceptionAndFinalizeAndRethrows()
    {
        var sut = new TestableHandler();
        var expectedException = new InvalidOperationException("fail");

        var act = () => sut.InvokeHandlingAsync<int>(() => throw expectedException);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal("fail", ex.Message);
        Assert.Same(expectedException, sut.ExceptionHandled);
        Assert.True(sut.FinalizeWasCalled);
    }

    private sealed class TestableHandler : BaseHandler
    {
        public bool FinalizeWasCalled { get; private set; }
        public Exception? ExceptionHandled { get; private set; }

        public Task InvokeHandlingAsync(Func<Task> action) => HandlingAsync(action);
        public Task<T> InvokeHandlingAsync<T>(Func<Task<T>> func) => HandlingAsync(func);

        protected override Task HandleExceptionAsync(Exception e)
        {
            ExceptionHandled = e;
            return Task.CompletedTask;
        }

        protected override Task FinalizeAsync()
        {
            FinalizeWasCalled = true;
            return Task.CompletedTask;
        }
    }
}
