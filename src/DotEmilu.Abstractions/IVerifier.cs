namespace DotEmilu.Abstractions;

public interface IVerifier<in TRequest> : IVerifier
    where TRequest : IBaseRequest
{
    Task ValidateAsync(TRequest request, CancellationToken cancellationToken);
}

public interface IVerifier
{
    IReadOnlyCollection<ValidationFailure> ValidationErrors { get; }
    bool IsValid { get; }
    void AddValidationErrors(in List<ValidationFailure> validationErrors);
    void AddValidationError(in ValidationFailure validationError);
    void AddValidationError(in string propertyName, in string errorMessage);
}