namespace DotEmilu.Samples.ConsoleApp.Scenarios.S01BasicHandler;

/// <summary>
/// Builds an isolated DI container for the S01BasicHandler scenario.
/// <para>
/// In a real application, each feature assembly contains exactly one handler per
/// request type, so <c>AddHandlers(Assembly.GetExecutingAssembly())</c> registers
/// only the handlers relevant to that assembly.  Here all scenarios share the same
/// assembly, but every request type maps to exactly one handler, so there is no
/// ambiguity in resolution.
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
