namespace DotEmilu.AspNetCore;

public static class AsDelegate
{
    /// <summary>
    /// Represents a method for handling asynchronous HTTP requests by using a handler function and optionally returning a result.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object. It must implement <see cref="IRequest"/>.</typeparam>
    /// <param name="result">
    /// An optional function representing a default result to return if the handler does not provide one.
    /// If not specified, the handler must provide the resulting <see cref="IResult"/>.
    /// </param>
    /// <returns>
    /// A function that processes the specified request asynchronously, using the provided handler and cancellation token,
    /// and returns an <see cref="IResult"/> based on the operation's outcome.
    /// </returns>
    public static Func<TRequest, HttpHandler<TRequest>, CancellationToken, Task<IResult>>
        ForAsync<TRequest>(Func<IResult>? result = null)
        where TRequest : IRequest
        => async (request, handler, cancellationToken) =>
            await handler.HandleAsync(request, cancellationToken, result);

    /// <summary>
    /// Represents a method for handling asynchronous HTTP requests by using a handler function
    /// and optionally returning a result based on the specified response type.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object. It must implement <see cref="IRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The type of the response object, representing the result to return.</typeparam>
    /// <param name="result">
    /// An optional function representing a default result to return if the handler does not provide one.
    /// If not specified, the handler must provide the resulting <see cref="IResult"/>.
    /// </param>
    /// <returns>
    /// A function that processes the specified request asynchronously, using the provided handler
    /// and cancellation token, and returns an <see cref="IResult"/> based on the operation's outcome.
    /// </returns>
    public static Func<TRequest, HttpHandler<TRequest, TResponse>, CancellationToken, Task<IResult>>
        ForAsync<TRequest, TResponse>(Func<TResponse?, IResult>? result = null)
        where TRequest : IRequest<TResponse>
        => async (request, handler, cancellationToken) =>
            await handler.HandleAsync(request, cancellationToken, result);
}