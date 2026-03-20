using DotEmilu.Samples.ConsoleApp.Scenarios;

using S01 = DotEmilu.Samples.ConsoleApp.Scenarios.S01BasicHandler;
using S02 = DotEmilu.Samples.ConsoleApp.Scenarios.S02HandlerWithResponse;
using S03 = DotEmilu.Samples.ConsoleApp.Scenarios.S03LifecycleHooks;
using S04 = DotEmilu.Samples.ConsoleApp.Scenarios.S04ManualValidationErrors;
using S05 = DotEmilu.Samples.ConsoleApp.Scenarios.S05ChainHandlerSimple;
using S06 = DotEmilu.Samples.ConsoleApp.Scenarios.S06ChainHandlerMultiStep;
using S07 = DotEmilu.Samples.ConsoleApp.Scenarios.S07OrchestratingHandler;
using S08 = DotEmilu.Samples.ConsoleApp.Scenarios.S08ParameterlessHandler;

// ─────────────────────────────────────────────────────────────────────────────
// DotEmilu.Samples.ConsoleApp
//
// Each scenario is completely self-contained:
//   • It builds its own ServiceCollection and ServiceProvider.
//   • It registers only the dependencies it actually needs.
//   • It disposes its container when done — zero shared state between scenarios.
//
// This mirrors how real applications work: each feature assembly registers its
// own handlers via AddHandlers(Assembly.GetExecutingAssembly()), validators via
// AddValidatorsFromAssembly(), and chain handlers via AddChainHandlers().
// ─────────────────────────────────────────────────────────────────────────────

IScenario[] scenarios =
[
    new S01.Scenario(),
    new S02.Scenario(),
    new S03.Scenario(),
    new S04.Scenario(),
    new S05.Scenario(),
    new S06.Scenario(),
    new S07.Scenario(),
    new S08.Scenario(),
];

foreach (var scenario in scenarios)
    await scenario.RunAsync();

Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════");
Console.WriteLine("  🏁  All ConsoleApp scenarios completed.");
Console.WriteLine("══════════════════════════════════════════");
