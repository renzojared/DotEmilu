namespace DotEmilu.Abstractions;

public interface IVerifier<in TRequest> : IVerifier
    where TRequest : IBaseRequest
{
    Task ValidateAsync(TRequest request, CancellationToken cancellationToken);
}

public interface IVerifier
{
    IReadOnlyCollection<ValidationFailure> Errors { get; }
    bool IsValid { get; }
    void AddErrors(in List<ValidationFailure> errors);
    void AddError(in ValidationFailure error);
    void AddError(in string propertyName, in string errorMessage);
}