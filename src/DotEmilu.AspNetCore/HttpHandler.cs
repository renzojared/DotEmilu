namespace DotEmilu.AspNetCore;

public class HttpHandler<TRequest>(IHandler<TRequest> handler, IVerifier<TRequest> verifier, IPresenter presenter)
    where TRequest : IRequest
{
    public async Task<IResult> HandleAsync(TRequest request, CancellationToken cancellationToken,
        Func<IResult>? result = null)
    {
        try
        {
            await handler.HandleAsync(request, cancellationToken);

            if (!verifier.IsValid)
                return presenter.ValidationError(verifier.Errors);

            return result is not null ? result.Invoke() : Results.Ok();
        }
        catch (Exception e)
        {
            return presenter.ServerError(e);
        }
    }
}

public class HttpHandler<TRequest, TResponse>(
    IHandler<TRequest, TResponse> handler,
    IVerifier<TRequest> verifier,
    IPresenter presenter)
    where TRequest : IRequest<TResponse>
{
    public async Task<IResult> HandleAsync(TRequest request, CancellationToken cancellationToken,
        Func<TResponse?, IResult>? result = null)
    {
        try
        {
            var response = await handler.HandleAsync(request, cancellationToken);

            if (!verifier.IsValid)
                return presenter.ValidationError(verifier.Errors);

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