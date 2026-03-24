namespace DotEmilu.UnitTests.Core;

public class OrchestratingHandlerTests
{
    private readonly IVerifier<OuterRequest> _outerVerifier = Substitute.For<IVerifier<OuterRequest>>();
    private readonly IVerifier<InnerRequest> _innerVerifier = Substitute.For<IVerifier<InnerRequest>>();

    [Fact]
    public async Task HandleAsync_WhenSubHandlerSucceeds_ThenReturnsResponse()
    {
        _outerVerifier.IsValid.Returns(true);
        _innerVerifier.IsValid.Returns(true);
        var innerHandler = new InnerHandler(_innerVerifier);
        var sut = new OuterHandler(_outerVerifier, innerHandler, _innerVerifier);

        var result = await sut.HandleAsync(new OuterRequest("ok"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("result:ok", result.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenSubHandlerAddsValidationErrors_ThenPropagatesErrorsToOuterVerifier()
    {
        _outerVerifier.IsValid.Returns(true);
        _innerVerifier.IsValid.Returns(false);
        _innerVerifier.ValidationErrors.Returns(
            new[] { new ValidationFailure("Field", "inner error") }.ToList().AsReadOnly());
        var innerHandler = new InnerHandler(_innerVerifier);
        var sut = new OuterHandler(_outerVerifier, innerHandler, _innerVerifier);

        await sut.HandleAsync(new OuterRequest("fail"), CancellationToken.None);

        _outerVerifier.Received(1).AddValidationErrors(
            Arg.Is<List<ValidationFailure>>(list => list.Any(f => f.PropertyName == "Field")));
    }

    [Fact]
    public async Task HandleAsync_WhenSubHandlerAddsValidationErrors_ThenReturnsNull()
    {
        _outerVerifier.IsValid.Returns(true);
        _innerVerifier.IsValid.Returns(false);
        _innerVerifier.ValidationErrors.Returns(
            new[] { new ValidationFailure("Field", "inner error") }.ToList().AsReadOnly());
        var innerHandler = new InnerHandler(_innerVerifier);
        var sut = new OuterHandler(_outerVerifier, innerHandler, _innerVerifier);

        var result = await sut.HandleAsync(new OuterRequest("fail"), CancellationToken.None);

        Assert.Null(result);
    }

    public sealed record OuterRequest(string Payload) : IRequest<OuterResponse>;

    public sealed record OuterResponse(string Value);

    public sealed record InnerRequest(string Payload) : IRequest;

    private sealed class InnerHandler(IVerifier<InnerRequest> verifier) : Handler<InnerRequest>(verifier)
    {
        protected override Task HandleUseCaseAsync(InnerRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class OuterHandler(
        IVerifier<OuterRequest> verifier,
        IHandler<InnerRequest> subHandler,
        IVerifier<InnerRequest> subVerifier)
        : Handler<OuterRequest, OuterResponse>(verifier)
    {
        private readonly IVerifier<OuterRequest> _verifier = verifier;

        protected override async Task<OuterResponse?> HandleUseCaseAsync(
            OuterRequest request,
            CancellationToken cancellationToken)
        {
            await subHandler.HandleAsync(new InnerRequest(request.Payload), cancellationToken);

            if (!subVerifier.IsValid)
            {
                _verifier.AddValidationErrors(subVerifier.ValidationErrors.ToList());
                return null;
            }

            return new OuterResponse($"result:{request.Payload}");
        }
    }
}
