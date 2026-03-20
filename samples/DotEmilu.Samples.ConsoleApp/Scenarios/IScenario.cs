namespace DotEmilu.Samples.ConsoleApp.Scenarios;

/// <summary>
/// Contract for a self-contained console scenario.
/// Each implementation builds its own DI container and runs in complete isolation.
/// </summary>
public interface IScenario
{
    /// <summary>Executes the scenario asynchronously.</summary>
    Task RunAsync();
}
