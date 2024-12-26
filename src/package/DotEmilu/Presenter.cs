namespace DotEmilu;

internal sealed class Presenter(IOptions<ResultMessage> options) : IPresenter
{
    public IResult ValidationError(in IEnumerable<ValidationFailure> validationFailures)
        => Results.ValidationProblem(
            errors: validationFailures.GroupBy(f => f.PropertyName, f => f.ErrorMessage)
                .ToDictionary(d => d.Key, d => d.ToArray()),
            detail: options.Value.ValidationError.Detail,
            instance: $"{nameof(ProblemDetails)}/{nameof(ValidationProblemDetails)}",
            statusCode: StatusCodes.Status400BadRequest,
            title: options.Value.ValidationError.Title,
            type: options.Value.ValidationError.Type ?? "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1");

    public IResult ServerError(in Exception exception)
        => Results.Problem(
            detail: options.Value.ServerError.Detail,
            instance: $"{nameof(ProblemDetails)}/{exception.GetType().Name}",
            statusCode: StatusCodes.Status500InternalServerError,
            title: options.Value.ServerError.Title,
            type: options.Value.ServerError.Type ?? "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1");
}