namespace DotEmilu;

public interface IVerifier<in TRequest> : IVerifierError
{
    Task ValidateAsync(TRequest request, CancellationToken cancellationToken);
}