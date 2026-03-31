namespace DotEmilu.UnitTests.Core;

public class HandlerWithRequestTests
{
    private readonly IVerifier<TestRequest> _verifier = Substitute.For<IVerifier<TestRequest>>();

    [Fact]
    public async Task HandleAsync_WhenValid_ThenCallsUseCase()
    {
        _verifier.IsValid.Returns(true);
        var sut = new TestHandler(_verifier);

        await sut.HandleAsync(new TestRequest(), CancellationToken.None);

        Assert.True(sut.UseCaseWasCalled);
    }

    [Fact]
    public async Task HandleAsync_WhenCalled_ThenAlwaysValidatesRequestWithToken()
    {
        _verifier.IsValid.Returns(true);
        var sut = new TestHandler(_verifier);
        var request = new TestRequest();
        using var cts = new CancellationTokenSource();

        await sut.HandleAsync(request, cts.Token);

        await _verifier.Received(1).ValidateAsync(request, cts.Token);
        Assert.Equal(cts.Token, sut.UseCaseCancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WhenInvalid_ThenSkipsUseCase()
    {
        _verifier.IsValid.Returns(false);
        var sut = new TestHandler(_verifier);

        await sut.HandleAsync(new TestRequest(), CancellationToken.None);

        Assert.False(sut.UseCaseWasCalled);
    }

    [Fact]
    public async Task HandleAsync_WhenUseCaseThrows_ThenCallsHandleExceptionAndRethrows()
    {
        _verifier.IsValid.Returns(true);
        var sut = new ThrowingHandler(_verifier);

        var act = () => sut.HandleAsync(new TestRequest(), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.NotNull(sut.ExceptionHandled);
    }

    [Fact]
    public async Task HandleAsync_WhenUseCaseThrows_ThenAlwaysCallsFinalizeAsync()
    {
        _verifier.IsValid.Returns(true);
        var sut = new ThrowingHandler(_verifier);

        try
        {
            await sut.HandleAsync(new TestRequest(), CancellationToken.None);
        }
        catch
        {
            /* expected */
        }

        Assert.True(sut.FinalizeWasCalled);
    }

    public record TestRequest : IRequest;

    private sealed class TestHandler(IVerifier<TestRequest> verifier) : Handler<TestRequest>(verifier)
    {
        public bool UseCaseWasCalled { get; private set; }
        public CancellationToken? UseCaseCancellationToken { get; private set; }

        protected override Task HandleUseCaseAsync(TestRequest request, CancellationToken cancellationToken)
        {
            UseCaseWasCalled = true;
            UseCaseCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler(IVerifier<TestRequest> verifier) : Handler<TestRequest>(verifier)
    {
        public Exception? ExceptionHandled { get; private set; }
        public bool FinalizeWasCalled { get; private set; }

        protected override Task HandleUseCaseAsync(TestRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("test");

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
