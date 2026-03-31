using DotEmilu.EntityFrameworkCore;
using DotEmilu.Samples.Domain.Entities;
using DotEmilu.Samples.EntityFrameworkCore.DataAccess;
using DotEmilu.Samples.EntityFrameworkCore.DataAccess.CqrsPattern;
using DotEmilu.Samples.EntityFrameworkCore.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.Samples.EntityFrameworkCore.Scenarios.S05CqrsExecution;

internal sealed class Runner : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("S05", "CQRS Pattern — ICommands (write) and IQueries (read, NoTracking)");

        var services = new ServiceCollection();

        // ── Interceptors ─────────────────────────────────────────────────────
        services
            .AddSoftDeleteInterceptor()
            .AddAuditableEntityInterceptor<MockContextUser, Guid>();

        // ── DbContext ─────────────────────────────────────────────────────────
        // AddDbContext registers DbContextOptions<InvoiceDbContext> as scoped.
        // Both Commands and Queries inherit from InvoiceDbContext and receive those
        // same options through DI — no manual `new` required.
        services.AddDbContext<InvoiceDbContext>((sp, options) =>
            options
                .UseInMemoryDatabase("S05CqrsExecution")
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        // ── CQRS sides ────────────────────────────────────────────────────────
        services.AddScoped<ICommands, Commands>();
        services.AddScoped<IQueries, Queries>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var commands = scope.ServiceProvider.GetRequiredService<ICommands>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueries>();

        // IUnitOfWork.Database is part of the write-side contract — no downcast needed here.
        await commands.Database.EnsureCreatedAsync();

        // ── Step 1: Write via ICommands ─────────────────────────────────────
        Print.Step("1", "Write via ICommands — SaveChangesAsync() + interceptors populate audit fields");

        commands.Invoices.Add(new Invoice
        {
            Number = "CQRS-001",
            Description = "CQRS write demo",
            Amount = 500,
            Date = new DateOnly(2025, 5, 1),
            IsPaid = false
        });

        commands.Invoices.Add(new Invoice
        {
            Number = "CQRS-002",
            Description = "CQRS read demo",
            Amount = 750,
            Date = new DateOnly(2025, 5, 2),
            IsPaid = true
        });

        var saved = await commands.SaveChangesAsync();
        Console.WriteLine($"  ✅ Saved {saved} rows via ICommands.SaveChangesAsync()");

        // Downcast for demo only — production code should never reference the concrete type through
        // the CQRS interface. ChangeTracker is not part of IUnitOfWork by design.
        var cmdCtx = (InvoiceDbContext)commands;
        Console.WriteLine(
            $"  Commands ChangeTracker entries: {cmdCtx.ChangeTracker.Entries().Count()} (tracked after save)");

        // ── Step 2: Read via IQueries (NoTracking) ──────────────────────────
        Print.Step("2", "Read via IQueries — QueryTrackingBehavior.NoTracking, audit fields populated by interceptor");

        var invoices = await queries.Invoices.ToListAsync();
        Console.WriteLine($"  Found {invoices.Count} invoices via IQueries:");
        foreach (var inv in invoices)
            Console.WriteLine(
                $"    📄 {inv.Number}: {inv.Description} | Created: {inv.Created:u} | CreatedBy: {inv.CreatedBy}");

        // Downcast for demo only — same caveat as above.
        var qryCtx = (InvoiceDbContext)queries;
        Console.WriteLine(
            $"  Queries ChangeTracker entries: {qryCtx.ChangeTracker.Entries().Count()} (NoTracking — always 0)");

        // ── Step 3: Interface segregation ───────────────────────────────────
        Print.Step("3", "Interface segregation — compile-time safety");

        Console.WriteLine("  ICommands : IEntities, IUnitOfWork  →  exposes SaveChangesAsync ✅");
        Console.WriteLine("  IQueries  : IEntities               →  no SaveChangesAsync      ✅");
        Console.WriteLine("  The read side cannot accidentally mutate state — enforced by the type system.");

        // ── Step 4: Database facade available through IUnitOfWork ────────────
        Print.Step("4", "IUnitOfWork.Database is part of the write-side contract");

        Console.WriteLine($"  commands.Database.ProviderName: {commands.Database.ProviderName}");
    }
}
