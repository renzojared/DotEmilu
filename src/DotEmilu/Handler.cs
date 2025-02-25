namespace DotEmilu;

public abstract class Handler<TRequest>(IVerifier<TRequest> verifier)
    : BaseHandler, IHandler<TRequest>
    where TRequest : IRequest
{
    public async Task HandleAsync(TRequest request, CancellationToken cancellationToken)
        => await HandlingAsync(async () =>
        {
            await verifier.ValidateAsync(request, cancellationToken);

            if (!verifier.IsValid)
                return;

            await HandleUseCaseAsync(request, cancellationToken);
        });

    protected abstract Task HandleUseCaseAsync(TRequest request, CancellationToken cancellationToken);
}

public abstract class Handler<TRequest, TResponse>(IVerifier<TRequest> verifier)
    : BaseHandler, IHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse?> HandleAsync(TRequest request, CancellationToken cancellationToken)
        => await HandlingAsync(async () =>
        {
            await verifier.ValidateAsync(request, cancellationToken);

            if (!verifier.IsValid)
                return default;

            return await HandleUseCaseAsync(request, cancellationToken);
        });

    protected abstract Task<TResponse?> HandleUseCaseAsync(TRequest request, CancellationToken cancellationToken);
}

public abstract class BaseHandler
{
    protected virtual Task HandleExceptionAsync(Exception e) => Task.CompletedTask;
    protected virtual Task FinalizeAsync() => Task.CompletedTask;

    protected async Task HandlingAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e)
        {
            await HandleExceptionAsync(e);
            throw;
        }
        finally
        {
            await FinalizeAsync();
        }
    }

    protected async Task<TResponse> HandlingAsync<TResponse>(Func<Task<TResponse>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception e)
        {
            await HandleExceptionAsync(e);
            throw;
        }
        finally
        {
            await FinalizeAsync();
        }
    }
}