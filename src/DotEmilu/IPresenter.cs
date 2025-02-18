namespace DotEmilu;

public interface IPresenter
{
    IResult Success<TResponse>(in TResponse response) => Results.Ok(response);
    IResult ValidationError(in IEnumerable<ValidationFailure> validationFailures);
    IResult ServerError(in Exception exception);
}