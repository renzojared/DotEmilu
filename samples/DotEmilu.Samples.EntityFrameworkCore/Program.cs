using DotEmilu.Samples.EntityFrameworkCore.Scenarios;
using S01 = DotEmilu.Samples.EntityFrameworkCore.Scenarios.S01AuditableExplicit;
using S02 = DotEmilu.Samples.EntityFrameworkCore.Scenarios.S02AuditableReflection;
using S03 = DotEmilu.Samples.EntityFrameworkCore.Scenarios.S03SoftDelete;
using S04 = DotEmilu.Samples.EntityFrameworkCore.Scenarios.S04PaginatedList;
using S05 = DotEmilu.Samples.EntityFrameworkCore.Scenarios.S05CqrsExecution;
using S06 = DotEmilu.Samples.EntityFrameworkCore.Scenarios.S06ModelConfiguration;
using S07 = DotEmilu.Samples.EntityFrameworkCore.Scenarios.S07ModelBuilderOverloads;

IScenario[] scenarios =
[
    new S01.Runner(),
    new S02.Runner(),
    new S03.Runner(),
    new S04.Runner(),
    new S05.Runner(),
    new S06.Runner(),
    new S07.Runner(),
];

foreach (var scenario in scenarios)
    await scenario.RunAsync();

Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════");
Console.WriteLine("  🏁  All EntityFrameworkCore scenarios completed.");
Console.WriteLine("══════════════════════════════════════════");
