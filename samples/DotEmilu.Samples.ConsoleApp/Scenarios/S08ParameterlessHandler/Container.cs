namespace DotEmilu.Samples.ConsoleApp.Scenarios.S08ParameterlessHandler;

/// <summary>
/// Builds an isolated DI container for the S08ParameterlessHandler scenario.
/// <para>
/// <see cref="SeedDataHandler"/> implements the non-generic <see cref="IHandler"/>
/// interface, which <c>AddHandlers(assembly)</c> does <b>not</b> scan for — that
/// method only discovers <c>IHandler&lt;TRequest&gt;</c> and
/// <c>IHandler&lt;TRequest, TResponse&gt;</c> implementations.
/// </para>
/// <para>
/// As a result, parameterless handlers must be registered <b>explicitly</b> by their
/// concrete type.  Callers resolve the concrete class directly from the container
/// (no interface ambiguity since there is no typed service key to look up):
/// <code>
/// var handler = scope.ServiceProvider.GetRequiredService&lt;SeedDataHandler&gt;();
/// await handler.HandleAsync(cancellationToken);
/// </code>
/// </para>
/// </summary>
internal static class Container
{
    internal static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        // Explicit registration — AddHandlers() would NOT discover this class
        // because IHandler (non-generic) is outside the reflection scan contract.
        services.AddScoped<SeedDataHandler>();

        return services.BuildServiceProvider();
    }
}
