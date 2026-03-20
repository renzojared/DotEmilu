namespace DotEmilu.Samples.ConsoleApp.Scenarios.S03LifecycleHooks;

/// <summary>
/// Builds an isolated DI container for the S03LifecycleHooks scenario.
/// </summary>
internal static class Container
{
    internal static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        services
            .AddValidatorsFromAssembly(assembly, includeInternalTypes:true)
            .AddVerifier()
            .AddHandlers(assembly);

        return services.BuildServiceProvider();
    }
}
