namespace DotEmilu.Samples.ConsoleApp.Scenarios.S06ChainHandlerMultiStep;

/// <summary>
/// Builds an isolated DI container for the S06ChainHandlerMultiStep scenario.
/// <para>
/// Three registrations are required:
/// <list type="bullet">
///   <item>
///     <c>AddHandlers(assembly)</c> — registers <see cref="SyncJobProcessor"/> as
///     <c>IHandler&lt;SyncJobContext&gt;</c>.  The processor implements the interface
///     directly (no base <c>Handler&lt;T&gt;</c>), so no verifier is needed for the
///     context itself.
///   </item>
///   <item>
///     <c>AddChainHandlers(assembly)</c> — registers <see cref="ValidateJobHandler"/>,
///     <see cref="EnrichJobHandler"/>, and <see cref="PersistJobHandler"/> by their
///     concrete types so they can be injected into <see cref="SyncJobProcessor"/>'s
///     constructor.
///   </item>
///   <item>
///     <c>AddVerifier()</c> — required by <c>AddHandlers</c> internally (the generic
///     <c>IVerifier&lt;&gt;</c> open registration is always safe to add).
///   </item>
/// </list>
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
            .AddHandlers(assembly)
            .AddChainHandlers(assembly);

        return services.BuildServiceProvider();
    }
}
