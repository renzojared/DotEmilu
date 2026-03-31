namespace DotEmilu.Samples.ConsoleApp.Scenarios.S08ParameterlessHandler;

/// <summary>
/// S08 — Parameterless Handler (<see cref="IHandler"/>).
/// <para>
/// Demonstrates the zero-argument variant of the handler contract.  Unlike
/// <c>IHandler&lt;TRequest&gt;</c>, this interface has no typed request and no
/// verifier — it is the right choice for fire-and-forget operations that carry
/// no input (e.g. cache warming, reference data seeding, scheduled cleanup jobs).
/// </para>
/// <para>
/// Key differences from a typed handler:
/// <list type="number">
///   <item>
///     <b>No <c>AddHandlers(assembly)</c> discovery</b> — the reflection scan in
///     <c>DiContainer</c> only finds generic <c>IHandler&lt;T&gt;</c> and
///     <c>IHandler&lt;T, TResponse&gt;</c> implementations.  The concrete type must
///     be registered explicitly in <see cref="Container"/>.
///   </item>
///   <item>
///     <b>Resolved by concrete type</b> — callers ask for
///     <c>GetRequiredService&lt;SeedDataHandler&gt;()</c> directly; there is no
///     interface key to resolve by because multiple parameterless handlers could
///     coexist in an application.
///   </item>
///   <item>
///     <b>No verifier</b> — there is nothing to validate when there is no input.
///   </item>
/// </list>
/// </para>
/// </summary>
internal sealed class Scenario : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("08", "Parameterless Handler (IHandler — no request, no verifier)");

        await using var provider = Container.Build();

        // ── Resolve by concrete type and invoke without a request object ─────
        Print.Step("A", "Resolve SeedDataHandler by concrete type and call HandleAsync()");

        await using (var scope = provider.CreateAsyncScope())
        {
            // Parameterless handlers are resolved by their concrete class —
            // there is no typed interface key (IHandler<TRequest>) to look up.
            var handler = scope.ServiceProvider.GetRequiredService<SeedDataHandler>();

            await handler.HandleAsync(CancellationToken.None);
        }

        // ── Run a second time to show the handler is stateless / re-usable ───
        Print.Step("B", "Second run — handler is stateless, each scope gets a fresh instance");

        await using (var scope = provider.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<SeedDataHandler>();

            await handler.HandleAsync(CancellationToken.None);

            Console.WriteLine("  ℹ️  Each DI scope resolves an independent handler instance.");
        }
    }
}
