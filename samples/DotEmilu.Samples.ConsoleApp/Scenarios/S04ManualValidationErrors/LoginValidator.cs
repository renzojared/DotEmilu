namespace DotEmilu.Samples.ConsoleApp.Scenarios.S04ManualValidationErrors;

/// <summary>
/// Validates the structure of a <see cref="LoginRequest"/> using FluentValidation.
/// <para>
/// Structural rules (non-empty, length) are expressed here.  Semantic rules
/// (are the credentials correct?) are evaluated at runtime inside
/// <see cref="LoginHandler.HandleUseCaseAsync"/> via
/// <c>_verifier.AddValidationError()</c>.
/// </para>
/// </summary>
internal sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(4).WithMessage("Password must be at least 4 characters.");
    }
}
