namespace DotEmilu.Abstractions;

/// <summary>
/// Represents a validator for a specific request type.
/// </summary>
/// <typeparam name="TRequest">The type of the request to validate.</typeparam>
public interface IVerifier<in TRequest> : IVerifier
    where TRequest : IBaseRequest
{
    /// <summary>Validates the specified request asynchronously.</summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    Task ValidateAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Represents the base validation result container.
/// </summary>
public interface IVerifier
{
    /// <summary>Gets the collection of validation errors.</summary>
    IReadOnlyCollection<ValidationFailure> ValidationErrors { get; }

    /// <summary>Gets a value indicating whether the validation succeeded.</summary>
    bool IsValid { get; }

    /// <summary>Adds a collection of validation errors.</summary>
    /// <param name="validationErrors">The errors to add.</param>
    void AddValidationErrors(in List<ValidationFailure> validationErrors);

    /// <summary>Adds a single validation error.</summary>
    /// <param name="validationError">The error to add.</param>
    void AddValidationError(in ValidationFailure validationError);

    /// <summary>Adds a single validation error for a specific property.</summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="errorMessage">The error message.</param>
    void AddValidationError(in string propertyName, in string errorMessage);
}