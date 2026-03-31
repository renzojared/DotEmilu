namespace DotEmilu.Samples.ConsoleApp.Scenarios.S07OrchestratingHandler;

/// <summary>
/// Builds an isolated DI container for the S07OrchestratingHandler scenario.
/// <para>
/// A single <c>AddHandlers(assembly)</c> call registers both the outer
/// <see cref="ProcessOrderHandler"/> (<c>IHandler&lt;ProcessOrderRequest, ProcessOrderResult&gt;</c>)
/// and the inner <see cref="ValidateOrderHandler"/> (<c>IHandler&lt;ValidateOrderRequest&gt;</c>)
/// because they both implement a recognised handler interface and live in the same assembly.
/// </para>
/// <para>
/// <c>AddVerifier()</c> registers the open-generic <c>IVerifier&lt;&gt;</c> so that both
/// <c>IVerifier&lt;ProcessOrderRequest&gt;</c> and <c>IVerifier&lt;ValidateOrderRequest&gt;</c>
/// are available for injection — the outer handler uses its own verifier, the inner handler
/// uses its own, and the outer handler reads the inner verifier to propagate errors.
/// </para>
/// </summary>
internal static class Container
{
    internal static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        services
            .AddValidatorsFromAssembly(assembly, includeInternalTypes: true)
            .AddVerifier()
            .AddHandlers(assembly);

        return services.BuildServiceProvider();
    }
}
