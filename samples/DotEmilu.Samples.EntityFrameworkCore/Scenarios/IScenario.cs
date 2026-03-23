namespace DotEmilu.Samples.EntityFrameworkCore.Scenarios;

/// <summary>Represents a self-contained runnable scenario.</summary>
public interface IScenario
{
    /// <summary>Executes the scenario asynchronously.</summary>
    Task RunAsync();
}
