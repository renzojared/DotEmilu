namespace DotEmilu.Samples.ConsoleApp.Scenarios.S05ChainHandlerSimple;

/// <summary>
/// Builds an isolated DI container for the S05ChainHandlerSimple scenario.
/// <para>
/// <c>AddChainHandlers(assembly)</c> scans for all concrete classes that inherit
/// from <see cref="ChainHandler{TChain}"/> and registers them by their concrete type
/// (not by an interface), so they can be injected directly into the caller that wires
/// up the chain.
/// </para>
/// </summary>
internal static class Container
{
    internal static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        services
            .AddVerifier()
            .AddChainHandlers(assembly);

        return services.BuildServiceProvider();
    }
}
