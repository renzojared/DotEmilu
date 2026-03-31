namespace DotEmilu.AspNetCore;

/// <summary>
/// Wraps an application handler to process HTTP requests and produce HTTP results.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
public class HttpHandler<TRequest>(IHandler<TRequest> handler, IVerifier<TRequest> verifier, IPresenter presenter)
    where TRequest : IRequest
{
    /// <summary>Handles the HTTP request asynchronously.</summary>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="result">An optional function to provide a custom HTTP result.</param>
    /// <returns>A task representing the asynchronous operation, containing the HTTP result.</returns>
    public async Task<IResult> HandleAsync(TRequest request, CancellationToken cancellationToken,
        Func<IResult>? result = null)
    {
        try
        {
            await handler.HandleAsync(request, cancellationToken);

            if (!verifier.IsValid)
                return presenter.ValidationError(verifier.ValidationErrors);

            return result is not null ? result.Invoke() : TypedResults.Ok();
        }
        catch (Exception e)
        {
            return presenter.ServerError(e);
        }
    }
}

/// <summary>
/// Wraps an application handler to process HTTP requests and produce HTTP results with a payload.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response payload.</typeparam>
public class HttpHandler<TRequest, TResponse>(
    IHandler<TRequest, TResponse> handler,
    IVerifier<TRequest> verifier,
    IPresenter presenter)
    where TRequest : IRequest<TResponse>
{
    /// <summary>Handles the HTTP request asynchronously.</summary>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="result">An optional function to provide a custom HTTP result based on the response.</param>
    /// <returns>A task representing the asynchronous operation, containing the HTTP result.</returns>
    public async Task<IResult> HandleAsync(TRequest request, CancellationToken cancellationToken,
        Func<TResponse?, IResult>? result = null)
    {
        try
        {
            var response = await handler.HandleAsync(request, cancellationToken);

            if (!verifier.IsValid)
                return presenter.ValidationError(verifier.ValidationErrors);

            if (result is not null)
                return result.Invoke(response);

            ArgumentNullException.ThrowIfNull(response);

            return presenter.Success(response);
        }
        catch (Exception e)
        {
            return presenter.ServerError(e);
        }
    }
}