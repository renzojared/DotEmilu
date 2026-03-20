namespace DotEmilu.Samples.ConsoleApp.Scenarios.S04ManualValidationErrors;

/// <summary>
/// S04 — Manual Validation Errors via <c>IVerifier.AddValidationError()</c>.
/// <para>
/// Shows the three distinct validation paths a handler can produce:
/// <list type="number">
///   <item>
///     <b>Structural failure</b> — FluentValidation rejects an empty request before the
///     use-case body even runs.
///   </item>
///   <item>
///     <b>Semantic failure</b> — the request is structurally valid but the runtime check
///     (credential lookup) fails; the handler adds the error manually via
///     <c>_verifier.AddValidationError()</c> and returns <c>null</c>.
///   </item>
///   <item>
///     <b>Success</b> — valid structure <em>and</em> correct credentials; the handler
///     returns a populated <see cref="LoginResult"/>.
///   </item>
/// </list>
/// </para>
/// </summary>
internal sealed class Scenario : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("04", "Manual Validation Errors (verifier.AddValidationError inside use case)");

        await using var provider = Container.Build();

        // ── Path A: STRUCTURAL failure (FluentValidation) ────────────────────
        Print.Step("A", "Structurally invalid request — FluentValidation blocks execution");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider
                .GetRequiredService<IHandler<LoginRequest, LoginResult>>();
            var verifier = scope.ServiceProvider
                .GetRequiredService<IVerifier<LoginRequest>>();

            var empty = new LoginRequest(Username: "", Password: "");

            var result = await handler.HandleAsync(empty, CancellationToken.None);

            Console.WriteLine($"  📦 Result: {result ?? (object)"(null — validation blocked execution)"}");

            if (!verifier.IsValid)
            {
                Console.WriteLine("  ❌ FluentValidation errors:");
                foreach (var error in verifier.ValidationErrors)
                    Console.WriteLine($"     • {error.PropertyName}: {error.ErrorMessage}");
            }
        }

        // ── Path B: SEMANTIC failure (manual error from inside the use case) ─
        Print.Step("B", "Structurally valid — bad credentials → handler adds error manually");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider
                .GetRequiredService<IHandler<LoginRequest, LoginResult>>();
            var verifier = scope.ServiceProvider
                .GetRequiredService<IVerifier<LoginRequest>>();

            var wrongCredentials = new LoginRequest(Username: "admin", Password: "wrong");

            var result = await handler.HandleAsync(wrongCredentials, CancellationToken.None);

            Console.WriteLine($"  📦 Result: {result ?? (object)"(null — semantic validation failed)"}");

            if (!verifier.IsValid)
            {
                Console.WriteLine("  ❌ Business validation errors:");
                foreach (var error in verifier.ValidationErrors)
                    Console.WriteLine($"     • {error.PropertyName}: {error.ErrorMessage}");
            }
        }

        // ── Path C: SUCCESS ──────────────────────────────────────────────────
        Print.Step("C", "Valid structure + correct credentials → handler returns LoginResult");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider
                .GetRequiredService<IHandler<LoginRequest, LoginResult>>();

            var valid = new LoginRequest(Username: "admin", Password: "s3cr3t");

            await handler.HandleAsync(valid, CancellationToken.None);
        }
    }
}
