namespace DotEmilu;

internal sealed class Verifier<TRequest>(IEnumerable<IValidator<TRequest>> validators) : IVerifier<TRequest>
{
    public List<ValidationFailure> Errors { get; private set; } = [];

    public async Task ValidateAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (validators.Any())
        {
            var validationResults =
                await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, cancellationToken)));

            Errors = validationResults
                .Where(e => e.Errors.Count != 0)
                .SelectMany(r => r.Errors)
                .ToList();
        }
    }
}