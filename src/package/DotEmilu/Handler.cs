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
            await HandleExceptionAsync(ref e, cancellationToken);
            return presenter.ServerError(e);
        }
        finally
        {
            await FinalizeAsync(cancellationToken);
        }
    }

    protected abstract Task<IResult> HandleUseCaseAsync(TRequest request,
        CancellationToken cancellationToken = default);

    protected virtual Task HandleExceptionAsync(ref Exception e, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    protected virtual Task FinalizeAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}