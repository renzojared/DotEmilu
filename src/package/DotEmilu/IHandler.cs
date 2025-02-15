namespace DotEmilu;

public interface IHandler<in TRequest>
{
    Task<IResult> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

public interface IHandler
{
    Task HandleAsync(CancellationToken cancellationToken);
}