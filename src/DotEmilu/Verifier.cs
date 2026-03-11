namespace DotEmilu;

internal sealed class Verifier<TRequest>(IEnumerable<IValidator<TRequest>> validators) : IVerifier<TRequest>
    where TRequest : IBaseRequest
{
    private readonly List<ValidationFailure> _validationErrors = [];
    public IReadOnlyCollection<ValidationFailure> ValidationErrors => _validationErrors;
    public bool IsValid => ValidationErrors.Count == 0;

    public async Task ValidateAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var validationResults =
                await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, cancellationToken)));

            _validationErrors.AddRange(validationResults
                .Where(e => e.Errors.Count != 0)
                .SelectMany(r => r.Errors));
        }
    }

    public void AddValidationErrors(in List<ValidationFailure> validationErrors)
        => _validationErrors.AddRange(validationErrors);

    public void AddValidationError(in ValidationFailure validationError)
        => _validationErrors.Add(validationError);

    public void AddValidationError(in string propertyName, in string errorMessage)
        => _validationErrors.Add(new ValidationFailure(propertyName, errorMessage));
}