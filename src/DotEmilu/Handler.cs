namespace DotEmilu;

public abstract class Handler<TRequest>(IVerifier<TRequest> verifier, IPresenter presenter)
    : IHandler<TRequest>
{
    public async Task<IResult> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await verifier.ValidateAsync(request, cancellationToken);

            if (!verifier.IsValid) return presenter.ValidationError(verifier.Errors);

            return await HandleUseCaseAsync(request, cancellationToken);
        }
        catch (Exception e)
        {
            await HandleExceptionAsync(ref e);
            return presenter.ServerError(e);
        }
        finally
        {
            await FinalizeAsync();
        }
    }

    protected abstract Task<IResult> HandleUseCaseAsync(TRequest request, CancellationToken cancellationToken);
    protected virtual Task HandleExceptionAsync(ref Exception e) => Task.CompletedTask;
    protected virtual Task FinalizeAsync() => Task.CompletedTask;
}

public abstract class Handler<TRequest, TResponse>(IVerifier<TRequest> verifier, IPresenter presenter)
    : Handler<TRequest>(verifier, presenter)
{
    private readonly IVerifierError _verifier = verifier;
    private readonly IPresenter _presenter = presenter;
    private IResult? Result { get; set; }

    protected sealed override async Task<IResult> HandleUseCaseAsync(TRequest request,
        CancellationToken cancellationToken)
    {
        var response = await HandleResponseAsync(request, cancellationToken);

        if (!_verifier.IsValid) return _presenter.ValidationError(_verifier.Errors);

        if (Result is not null) return Result;

        ArgumentNullException.ThrowIfNull(response);

        return _presenter.Success(response);
    }

    protected abstract Task<TResponse?> HandleResponseAsync(TRequest request, CancellationToken cancellationToken);

    protected TResponse? ResultIn(IResult result)
    {
        Result = result;
        return default;
    }
}