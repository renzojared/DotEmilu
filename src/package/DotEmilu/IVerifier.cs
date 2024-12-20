namespace DotEmilu;

public interface IVerifier<in TRequest>
{
    List<ValidationFailure> Errors { get; }
    bool IsValid => Errors.Count == 0;
    Task ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
    void AddErrors(in List<ValidationFailure> errors);
}