namespace DotEmilu;

public interface IVerifierError
{
    List<ValidationFailure> Errors { get; }
    bool IsValid => Errors.Count == 0;

    void AddErrors(in List<ValidationFailure> errors)
        => Errors.AddRange(errors);

    void AddError(in ValidationFailure error)
        => Errors.Add(error);

    void AddError(in string propertyName, in string errorMessage)
        => Errors.Add(new ValidationFailure(propertyName, errorMessage));
}