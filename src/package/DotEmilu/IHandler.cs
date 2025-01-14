namespace DotEmilu;

public interface IHandler<in TRequest>
{
    Task<IResult> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface IHandler
{
    Task HandleAsync(CancellationToken cancellationToken = default);
}