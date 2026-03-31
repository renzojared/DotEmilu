namespace DotEmilu.Samples.ConsoleApp.Scenarios;

/// <summary>
/// Provides consistent output formatting helpers for all console scenarios.
/// </summary>
internal static class Print
{
    private const string Separator = "══════════════════════════════════════════";

    /// <summary>Prints a scenario header with its number and title.</summary>
    internal static void Header(string number, string title)
    {
        Console.WriteLine();
        Console.WriteLine(Separator);
        Console.WriteLine($"  S{number} — {title}");
        Console.WriteLine(Separator);
    }

    /// <summary>Prints a sub-step label inside a scenario.</summary>
    internal static void Step(string label, string description)
    {
        Console.WriteLine();
        Console.WriteLine($"  ── {label}: {description}");
    }
}
