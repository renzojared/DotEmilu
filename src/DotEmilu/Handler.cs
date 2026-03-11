namespace DotEmilu;

/// <summary>
/// Base class for handling requests without a response.
/// Incorporates validation before executing the use case.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
public abstract class Handler<TRequest>(IVerifier<TRequest> verifier)
    : BaseHandler, IHandler<TRequest>
    where TRequest : IRequest
{
    /// <inheritdoc />
    public async Task HandleAsync(TRequest request, CancellationToken cancellationToken)
        => await HandlingAsync(async () =>
        {
            await verifier.ValidateAsync(request, cancellationToken);

            if (!verifier.IsValid)
                return;

            await HandleUseCaseAsync(request, cancellationToken);
        });

    /// <summary>Executes the core logic of the use case.</summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task HandleUseCaseAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Base class for handling requests with a response.
/// Incorporates validation before executing the use case.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public abstract class Handler<TRequest, TResponse>(IVerifier<TRequest> verifier)
    : BaseHandler, IHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse?> HandleAsync(TRequest request, CancellationToken cancellationToken)
        => await HandlingAsync(async () =>
        {
            await verifier.ValidateAsync(request, cancellationToken);

            if (!verifier.IsValid)
                return default;

            return await HandleUseCaseAsync(request, cancellationToken);
        });

    /// <summary>Executes the core logic of the use case.</summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the response.</returns>
    protected abstract Task<TResponse?> HandleUseCaseAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Base class providing error handling and finalization logic for handlers.
/// </summary>
public abstract class BaseHandler
{
    /// <summary>Handles any exception thrown during the execution.</summary>
    /// <param name="e">The exception.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task HandleExceptionAsync(Exception e) => Task.CompletedTask;

    /// <summary>Executes finalization logic regardless of success or failure.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task FinalizeAsync() => Task.CompletedTask;

    /// <summary>Wraps an asynchronous action with error handling and finalization.</summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>Wraps an asynchronous function with error handling and finalization.</summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="action">The function to execute.</param>
    /// <returns>A task representing the asynchronous operation, containing the response.</returns>
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