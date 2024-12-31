namespace DotEmilu;

public abstract class Handler<TRequest>(IVerifier<TRequest> verifier, IPresenter presenter) : IHandler<TRequest>
{
    public async Task<IResult> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
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

    protected abstract Task<IResult> HandleUseCaseAsync(TRequest request,
        CancellationToken cancellationToken = default);

    protected virtual Task HandleExceptionAsync(ref Exception e) => Task.CompletedTask;

    protected virtual Task FinalizeAsync() => Task.CompletedTask;
}

public abstract class Handler<TRequest, TResponse>(IVerifier<TRequest> verifier, IPresenter presenter)
    : Handler<TRequest>(verifier, presenter)
{
    private readonly IVerifierError _verifier = verifier;
    private readonly IPresenter _presenter = presenter;

    protected sealed override async Task<IResult> HandleUseCaseAsync(TRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await HandleResponseAsync(request, cancellationToken);

        return !_verifier.IsValid ? _presenter.ValidationError(_verifier.Errors) : _presenter.Success(response);
    }

    protected abstract Task<TResponse?> HandleResponseAsync(TRequest request,
        CancellationToken cancellationToken = default);
}