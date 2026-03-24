using System.Text;
using DotEmilu.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.UnitTests.AspNetCore;

public sealed class HttpHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenValid_ThenReturnsDefaultOk()
    {
        var request = new TestRequest();
        var appHandler = Substitute.For<IHandler<TestRequest>>();
        var verifier = Substitute.For<IVerifier<TestRequest>>();
        var presenter = new CapturePresenter();
        var sut = new HttpHandler<TestRequest>(appHandler, verifier, presenter);

        verifier.IsValid.Returns(true);
        verifier.ValidationErrors.Returns([]);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        var executed = await ResultExecution.ExecuteAsync(result);
        Assert.Equal(StatusCodes.Status200OK, executed.StatusCode);
        await appHandler.Received(1).HandleAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenInvalid_ThenReturnsPresenterValidationError()
    {
        var request = new TestRequest();
        var appHandler = Substitute.For<IHandler<TestRequest>>();
        var verifier = Substitute.For<IVerifier<TestRequest>>();
        var presenter = Substitute.For<IPresenter>();
        var sut = new HttpHandler<TestRequest>(appHandler, verifier, presenter);

        verifier.IsValid.Returns(false);
        var failures = new[] { new ValidationFailure("Field", "error") };
        verifier.ValidationErrors.Returns(failures);
        var expected = TypedResults.BadRequest();
        presenter.ValidationError(Arg.Any<IEnumerable<ValidationFailure>>()).Returns(expected);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        Assert.Same(expected, result);
        presenter.Received(1).ValidationError(failures);
    }

    [Fact]
    public async Task HandleAsync_WhenHandlerThrows_ThenReturnsPresenterServerError()
    {
        var request = new TestRequest();
        var appHandler = Substitute.For<IHandler<TestRequest>>();
        var verifier = Substitute.For<IVerifier<TestRequest>>();
        var presenter = Substitute.For<IPresenter>();
        var sut = new HttpHandler<TestRequest>(appHandler, verifier, presenter);

        var boom = new InvalidOperationException("boom");
        appHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns(_ => throw boom);
        var expected = TypedResults.Problem();
        presenter.ServerError(Arg.Any<Exception>()).Returns(expected);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        Assert.Same(expected, result);
        presenter.Received(1).ServerError(boom);
    }

    [Fact]
    public async Task HandleAsync_WhenHandlerAddsValidationError_ThenReturnsPresenterValidationError()
    {
        var verifier = VerifierFactory.BuildVerifier<ManualValidationRequest>();
        var appHandler = new ManualValidationHandler(verifier);
        var presenter = Substitute.For<IPresenter>();
        var sut = new HttpHandler<ManualValidationRequest>(appHandler, verifier, presenter);
        var expected = TypedResults.ValidationProblem(new Dictionary<string, string[]>());
        presenter.ValidationError(Arg.Any<IEnumerable<ValidationFailure>>()).Returns(expected);

        var result = await sut.HandleAsync(new ManualValidationRequest(), CancellationToken.None);

        Assert.Same(expected, result);
        presenter.Received(1).ValidationError(Arg.Is<IEnumerable<ValidationFailure>>(errors =>
            errors.Any(e => e.PropertyName == "BusinessRule")));
    }
}

public sealed class HttpHandlerWithResponseTests
{
    [Fact]
    public async Task HandleAsync_WhenValid_ThenUsesPresenterSuccess()
    {
        var request = new TestRequestWithResponse();
        var response = new TestResponse { Value = "ok" };
        var appHandler = Substitute.For<IHandler<TestRequestWithResponse, TestResponse>>();
        var verifier = Substitute.For<IVerifier<TestRequestWithResponse>>();
        var presenter = new CapturePresenter();
        var sut = new HttpHandler<TestRequestWithResponse, TestResponse>(appHandler, verifier, presenter);

        appHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns(response);
        verifier.IsValid.Returns(true);
        verifier.ValidationErrors.Returns([]);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        var executed = await ResultExecution.ExecuteAsync(result);
        Assert.Equal(StatusCodes.Status200OK, executed.StatusCode);
        Assert.Equal(response, presenter.LastSuccessPayload);
    }

    [Fact]
    public async Task HandleAsync_WhenResponseNullAndNoResultFunc_ThenReturnsServerError()
    {
        var request = new TestRequestWithResponse();
        var appHandler = Substitute.For<IHandler<TestRequestWithResponse, TestResponse>>();
        var verifier = Substitute.For<IVerifier<TestRequestWithResponse>>();
        var presenter = new CapturePresenter();
        var sut = new HttpHandler<TestRequestWithResponse, TestResponse>(appHandler, verifier, presenter);

        appHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns((TestResponse?)null);
        verifier.IsValid.Returns(true);
        verifier.ValidationErrors.Returns([]);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        Assert.NotNull(presenter.LastServerError);
        Assert.IsType<ArgumentNullException>(presenter.LastServerError);
        var executed = await ResultExecution.ExecuteAsync(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, executed.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenResultFuncProvided_ThenUsesResultFunction()
    {
        var request = new TestRequestWithResponse();
        var appHandler = Substitute.For<IHandler<TestRequestWithResponse, TestResponse>>();
        var verifier = Substitute.For<IVerifier<TestRequestWithResponse>>();
        var presenter = new CapturePresenter();
        var sut = new HttpHandler<TestRequestWithResponse, TestResponse>(appHandler, verifier, presenter);

        appHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns((TestResponse?)null);
        verifier.IsValid.Returns(true);
        verifier.ValidationErrors.Returns([]);

        var result = await sut.HandleAsync(request, CancellationToken.None, _ => TypedResults.NotFound());

        var executed = await ResultExecution.ExecuteAsync(result);
        Assert.Equal(StatusCodes.Status404NotFound, executed.StatusCode);
        Assert.Null(presenter.LastSuccessPayload);
    }

    [Fact]
    public async Task HandleAsync_WhenInvalid_ThenReturnsPresenterValidationError()
    {
        var request = new TestRequestWithResponse();
        var appHandler = Substitute.For<IHandler<TestRequestWithResponse, TestResponse>>();
        var verifier = Substitute.For<IVerifier<TestRequestWithResponse>>();
        var presenter = Substitute.For<IPresenter>();
        var sut = new HttpHandler<TestRequestWithResponse, TestResponse>(appHandler, verifier, presenter);
        var failures = new[] { new ValidationFailure("Field", "error") };
        var expected = TypedResults.BadRequest();

        appHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns(new TestResponse { Value = "ok" });
        verifier.IsValid.Returns(false);
        verifier.ValidationErrors.Returns(failures);
        presenter.ValidationError(Arg.Any<IEnumerable<ValidationFailure>>()).Returns(expected);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        Assert.Same(expected, result);
        presenter.Received(1).ValidationError(failures);
    }

    [Fact]
    public async Task HandleAsync_WhenHandlerThrows_ThenReturnsPresenterServerError()
    {
        var request = new TestRequestWithResponse();
        var appHandler = Substitute.For<IHandler<TestRequestWithResponse, TestResponse>>();
        var verifier = Substitute.For<IVerifier<TestRequestWithResponse>>();
        var presenter = Substitute.For<IPresenter>();
        var sut = new HttpHandler<TestRequestWithResponse, TestResponse>(appHandler, verifier, presenter);
        var expected = TypedResults.Problem();
        var boom = new InvalidOperationException("boom");

        appHandler.HandleAsync(request, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<TestResponse?>(boom));
        presenter.ServerError(Arg.Any<Exception>()).Returns(expected);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        Assert.Same(expected, result);
        presenter.Received(1).ServerError(boom);
    }
}

public sealed class AsDelegateTests
{
    [Fact]
    public async Task ForAsync_WhenCalled_ThenDelegatesToHttpHandlerAndProducesResult()
    {
        var request = new TestRequest();
        var appHandler = Substitute.For<IHandler<TestRequest>>();
        var verifier = Substitute.For<IVerifier<TestRequest>>();
        var presenter = new CapturePresenter();
        var httpHandler = new HttpHandler<TestRequest>(appHandler, verifier, presenter);

        verifier.IsValid.Returns(true);
        verifier.ValidationErrors.Returns([]);

        var del = AsDelegate.ForAsync<TestRequest>(() => TypedResults.Created("/test"));
        var result = await del(request, httpHandler, CancellationToken.None);

        var executed = await ResultExecution.ExecuteAsync(result);
        Assert.Equal(StatusCodes.Status201Created, executed.StatusCode);
    }

    [Fact]
    public async Task ForAsync_WhenCalledWithResponse_ThenDelegatesToHttpHandler()
    {
        var request = new TestRequestWithResponse();
        var response = new TestResponse { Value = "ok" };
        var appHandler = Substitute.For<IHandler<TestRequestWithResponse, TestResponse>>();
        var verifier = Substitute.For<IVerifier<TestRequestWithResponse>>();
        var presenter = new CapturePresenter();
        var httpHandler = new HttpHandler<TestRequestWithResponse, TestResponse>(appHandler, verifier, presenter);
        appHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns(response);
        verifier.IsValid.Returns(true);
        verifier.ValidationErrors.Returns([]);

        var del = AsDelegate.ForAsync<TestRequestWithResponse, TestResponse>(_ => TypedResults.Accepted("/sync"));
        var result = await del(request, httpHandler, CancellationToken.None);

        var executed = await ResultExecution.ExecuteAsync(result);
        Assert.Equal(StatusCodes.Status202Accepted, executed.StatusCode);
    }

    [Fact]
    public async Task ForAsync_WhenValidationFails_ThenDelegateReturnsValidationError()
    {
        var request = new TestRequest();
        var appHandler = Substitute.For<IHandler<TestRequest>>();
        var verifier = Substitute.For<IVerifier<TestRequest>>();
        var presenter = Substitute.For<IPresenter>();
        var httpHandler = new HttpHandler<TestRequest>(appHandler, verifier, presenter);

        verifier.IsValid.Returns(false);
        verifier.ValidationErrors.Returns([new ValidationFailure("Field", "required")]);
        presenter.ValidationError(Arg.Any<IEnumerable<ValidationFailure>>())
            .Returns(TypedResults.ValidationProblem(new Dictionary<string, string[]>()));

        var del = AsDelegate.ForAsync<TestRequest>();
        var result = await del(request, httpHandler, CancellationToken.None);

        var executed = await ResultExecution.ExecuteAsync(result);
        Assert.Equal(StatusCodes.Status400BadRequest, executed.StatusCode);
    }

    [Fact]
    public async Task ForAsync_WhenCalled_ThenPropagatesCancellationTokenToHandler()
    {
        var request = new TestRequest();
        var appHandler = Substitute.For<IHandler<TestRequest>>();
        var verifier = Substitute.For<IVerifier<TestRequest>>();
        var presenter = new CapturePresenter();
        var httpHandler = new HttpHandler<TestRequest>(appHandler, verifier, presenter);
        using var cts = new CancellationTokenSource();

        verifier.IsValid.Returns(true);
        verifier.ValidationErrors.Returns([]);

        var del = AsDelegate.ForAsync<TestRequest>();
        await del(request, httpHandler, cts.Token);

        await appHandler.Received(1).HandleAsync(request, cts.Token);
    }
}

public sealed record TestRequest : IRequest;
public sealed record ManualValidationRequest : IRequest;

public sealed record TestRequestWithResponse : IRequest<TestResponse>;

public sealed class TestResponse
{
    public string Value { get; init; } = string.Empty;
}

public sealed class ManualValidationHandler(IVerifier<ManualValidationRequest> verifier)
    : Handler<ManualValidationRequest>(verifier)
{
    private readonly IVerifier<ManualValidationRequest> _verifier = verifier;

    protected override Task HandleUseCaseAsync(ManualValidationRequest request, CancellationToken cancellationToken)
    {
        _verifier.AddValidationError("BusinessRule", "Manual validation failed.");
        return Task.CompletedTask;
    }
}

public sealed class CapturePresenter : IPresenter
{
    public object? LastSuccessPayload { get; private set; }
    public Exception? LastServerError { get; private set; }

    public IResult Success<TResponse>(in TResponse response)
    {
        LastSuccessPayload = response;
        return TypedResults.Ok(response);
    }

    public IResult ValidationError(in IEnumerable<ValidationFailure> validationFailures)
        => TypedResults.ValidationProblem(new Dictionary<string, string[]> { { "__", ["invalid"] } });

    public IResult ServerError(in Exception exception)
    {
        LastServerError = exception;
        return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);
    }
}

public static class ResultExecution
{
    public static async Task<(int StatusCode, string ContentType, string Body)> ExecuteAsync(IResult result)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = CreateServices();
        await using var bodyStream = new MemoryStream();
        httpContext.Response.Body = bodyStream;

        await result.ExecuteAsync(httpContext);

        bodyStream.Position = 0;
        using var reader = new StreamReader(bodyStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        return (httpContext.Response.StatusCode, httpContext.Response.ContentType ?? string.Empty, body);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.Configure<JsonOptions>(_ => { });
        return services.BuildServiceProvider();
    }
}

public static class VerifierFactory
{
    public static IVerifier<TRequest> BuildVerifier<TRequest>() where TRequest : IBaseRequest
    {
        var services = new ServiceCollection();
        services.AddVerifier();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IVerifier<TRequest>>();
    }
}
