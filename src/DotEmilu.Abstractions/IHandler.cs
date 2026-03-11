namespace DotEmilu.Abstractions;

/// <summary>
/// Represents a handler for a request that does not return a response.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
public interface IHandler<in TRequest>
    where TRequest : IRequest
{
    /// <summary>Handles the specified request asynchronously.</summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task HandleAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a handler for a request that returns a response.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Handles the specified request asynchronously.</summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation, containing the response.</returns>
    Task<TResponse?> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a parameterless handler that performs an action.
/// </summary>
public interface IHandler
{
    /// <summary>Executes the handler's action asynchronously.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task HandleAsync(CancellationToken cancellationToken);
}