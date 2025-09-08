namespace DotEmilu.AspNetCore;

public interface IPresenter
{
    IResult Success<TResponse>(in TResponse response) => TypedResults.Ok(response);
    IResult ValidationError(in IEnumerable<ValidationFailure> validationFailures);
    IResult ServerError(in Exception exception);
}