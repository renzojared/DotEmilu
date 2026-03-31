namespace DotEmilu.Samples.EntityFrameworkCore.Scenarios;

internal static class Print
{
    internal static void Header(string number, string title)
    {
        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine($"  {number}  {title}");
        Console.WriteLine("══════════════════════════════════════════");
    }

    internal static void Step(string label, string description)
    {
        Console.WriteLine();
        Console.WriteLine($"  ▶ Step {label}: {description}");
    }
}
