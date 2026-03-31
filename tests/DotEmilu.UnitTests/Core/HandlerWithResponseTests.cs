namespace DotEmilu.UnitTests.Core;

public class HandlerWithResponseTests
{
    private readonly IVerifier<TestRequest> _verifier = Substitute.For<IVerifier<TestRequest>>();

    [Fact]
    public async Task HandleAsync_WhenValid_ThenReturnsResponse()
    {
        _verifier.IsValid.Returns(true);
        var sut = new TestHandler(_verifier);

        var result = await sut.HandleAsync(new TestRequest("hello"), CancellationToken.None);

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public async Task HandleAsync_WhenCalled_ThenAlwaysValidatesRequestAndToken()
    {
        _verifier.IsValid.Returns(true);
        var sut = new TestHandler(_verifier);
        var request = new TestRequest("hello");
        using var cts = new CancellationTokenSource();

        await sut.HandleAsync(request, cts.Token);

        await _verifier.Received(1).ValidateAsync(request, cts.Token);
        Assert.Equal(cts.Token, sut.UseCaseCancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WhenInvalid_ThenReturnsDefault()
    {
        _verifier.IsValid.Returns(false);
        var sut = new TestHandler(_verifier);

        var result = await sut.HandleAsync(new TestRequest("hello"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_WhenUseCaseReturnsNull_ThenReturnsNull()
    {
        _verifier.IsValid.Returns(true);
        var sut = new NullReturningHandler(_verifier);

        var result = await sut.HandleAsync(new TestRequest("test"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_WhenUseCaseThrows_ThenCallsHandleExceptionAndRethrows()
    {
        _verifier.IsValid.Returns(true);
        var sut = new ThrowingHandler(_verifier);

        var act = () => sut.HandleAsync(new TestRequest("boom"), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.NotNull(sut.ExceptionHandled);
    }

    [Fact]
    public async Task HandleAsync_WhenUseCaseThrows_ThenAlwaysCallsFinalize()
    {
        _verifier.IsValid.Returns(true);
        var sut = new ThrowingHandler(_verifier);

        try
        {
            await sut.HandleAsync(new TestRequest("boom"), CancellationToken.None);
        }
        catch
        {
            /* expected */
        }

        Assert.True(sut.FinalizeWasCalled);
    }

    public record TestRequest(string Value) : IRequest<string>;

    private sealed class TestHandler(IVerifier<TestRequest> verifier) : Handler<TestRequest, string>(verifier)
    {
        public CancellationToken? UseCaseCancellationToken { get; private set; }

        protected override Task<string?> HandleUseCaseAsync(TestRequest request, CancellationToken cancellationToken)
        {
            UseCaseCancellationToken = cancellationToken;
            return Task.FromResult<string?>(request.Value.ToUpperInvariant());
        }
    }

    private sealed class NullReturningHandler(IVerifier<TestRequest> verifier)
        : Handler<TestRequest, string>(verifier)
    {
        protected override Task<string?> HandleUseCaseAsync(TestRequest request, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);
    }

    private sealed class ThrowingHandler(IVerifier<TestRequest> verifier) : Handler<TestRequest, string>(verifier)
    {
        public Exception? ExceptionHandled { get; private set; }
        public bool FinalizeWasCalled { get; private set; }

        protected override Task<string?> HandleUseCaseAsync(TestRequest request, CancellationToken cancellationToken)
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
