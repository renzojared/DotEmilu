namespace DotEmilu.AspNetCore;

/// <summary>
/// Defines a presenter responsible for formatting HTTP responses.
/// </summary>
public interface IPresenter
{
    /// <summary>Creates a successful HTTP result with the provided response payload.</summary>
    /// <remarks>The default implementation returns HTTP 200 (OK).</remarks>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="response">The response payload.</param>
    /// <returns>An HTTP result representing success.</returns>
    IResult Success<TResponse>(in TResponse response) => TypedResults.Ok(response);

    /// <summary>Creates an HTTP result representing a validation error.</summary>
    /// <param name="validationFailures">The collection of validation failures.</param>
    /// <returns>An HTTP result representing a bad request.</returns>
    IResult ValidationError(in IEnumerable<ValidationFailure> validationFailures);

    /// <summary>Creates an HTTP result representing an internal server error.</summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>An HTTP result representing an internal server error.</returns>
    IResult ServerError(in Exception exception);
}
