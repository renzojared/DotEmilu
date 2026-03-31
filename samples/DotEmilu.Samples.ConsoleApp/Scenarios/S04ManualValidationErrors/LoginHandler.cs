namespace DotEmilu.Samples.ConsoleApp.Scenarios.S04ManualValidationErrors;

/// <summary>
/// Handles user authentication by validating credentials at the use-case level.
/// <para>
/// Demonstrates <see cref="IVerifier.AddValidationError(in string, in string)"/> called
/// <em>from inside</em> <see cref="HandleUseCaseAsync"/> — a pattern used when a business
/// rule can only be evaluated at runtime (e.g. database lookup, external service call)
/// and cannot be expressed with a static FluentValidation rule.
/// </para>
/// <remarks>
/// <c>IVerifier&lt;T&gt;</c> is injected and stored as a field because the base
/// <c>Handler&lt;T&gt;</c> constructor parameter is not directly accessible after
/// construction.  Storing the reference gives the use-case clean, explicit access to
/// the verifier.
/// </remarks>
/// </summary>
internal sealed class LoginHandler(IVerifier<LoginRequest> verifier)
    : Handler<LoginRequest, LoginResult>(verifier)
{
    private readonly IVerifier<LoginRequest> _verifier = verifier;

    protected override Task<LoginResult?> HandleUseCaseAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        // Simulate a credential store — in production this would be an async DB / IdP call.
        if (request.Username.Equals("admin", StringComparison.OrdinalIgnoreCase)
            && request.Password == "s3cr3t")
        {
            var token = $"Bearer {Guid.NewGuid():N}";
            var result = new LoginResult(token, DateTimeOffset.UtcNow.AddHours(8));

            Console.WriteLine($"  ✅ Login succeeded for '{request.Username}'.");
            Console.WriteLine($"     Token  : {result.Token[..20]}…");
            Console.WriteLine($"     Expires: {result.ExpiresAt:u}");

            return Task.FromResult<LoginResult?>(result);
        }

        // Manually inject a validation error that FluentValidation cannot express:
        // the credentials are structurally valid but semantically wrong.
        _verifier.AddValidationError(
            nameof(request.Username),
            "Invalid username or password.");

        Console.WriteLine($"  ❌ Login failed for '{request.Username}' — bad credentials.");

        return Task.FromResult<LoginResult?>(null);
    }
}
