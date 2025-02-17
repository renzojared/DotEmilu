namespace DotEmilu;

internal sealed class Verifier<TRequest>(IEnumerable<IValidator<TRequest>> validators) : IVerifier<TRequest>
    where TRequest : IBaseRequest
{
    private readonly List<ValidationFailure> _errors = [];
    public IReadOnlyCollection<ValidationFailure> Errors => _errors;
    public bool IsValid => Errors.Count == 0;

    public async Task ValidateAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var validationResults =
                await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, cancellationToken)));

            _errors.AddRange(validationResults
                .Where(e => e.Errors.Count != 0)
                .SelectMany(r => r.Errors));
        }
    }

    public void AddErrors(in List<ValidationFailure> errors)
        => _errors.AddRange(errors);

    public void AddError(in ValidationFailure error)
        => _errors.Add(error);

    public void AddError(in string propertyName, in string errorMessage)
        => _errors.Add(new ValidationFailure(propertyName, errorMessage));
}