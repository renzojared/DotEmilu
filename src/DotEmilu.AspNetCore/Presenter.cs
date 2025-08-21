namespace DotEmilu.AspNetCore;

internal sealed class Presenter(IOptionsMonitor<ResultMessage> options, IHostEnvironment environment) : IPresenter
{
    public IResult ValidationError(in IEnumerable<ValidationFailure> validationFailures)
        => TypedResults.ValidationProblem(
            errors: validationFailures.GroupBy(f => f.PropertyName, f => f.ErrorMessage)
                .ToDictionary(d => d.Key, d => d.ToArray()),
            detail: options.CurrentValue.ValidationError.Detail,
            instance: $"{nameof(ProblemDetails)}/{nameof(ValidationProblemDetails)}",
            title: options.CurrentValue.ValidationError.Title,
            type: string.IsNullOrEmpty(options.CurrentValue.ValidationError.Type)
                ? "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"
                : options.CurrentValue.ValidationError.Type);

    public IResult ServerError(in Exception exception)
    {
        var additionalDetails = environment.IsDevelopment()
            ? $"{exception.InnerException?.Message ?? exception.Message}: {exception.StackTrace}"
            : string.Empty;

        return TypedResults.Problem(
            detail: string.Join(" | ", new[] { options.CurrentValue.ServerError.Detail, additionalDetails }
                .Where(s => !string.IsNullOrEmpty(s))),
            instance: $"{nameof(ProblemDetails)}/{exception.GetType().Name}",
            statusCode: StatusCodes.Status500InternalServerError,
            title: options.CurrentValue.ServerError.Title,
            type: string.IsNullOrEmpty(options.CurrentValue.ServerError.Type)
                ? "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
                : options.CurrentValue.ServerError.Type);
    }
}