namespace DotEmilu.Samples.ConsoleApp.Scenarios.S02HandlerWithResponse;

/// <summary>
/// Builds an isolated DI container for the S02HandlerWithResponse scenario.
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
