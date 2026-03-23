using DotEmilu.EntityFrameworkCore;
using DotEmilu.Samples.Domain.Entities;
using DotEmilu.Samples.EntityFrameworkCore.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.Samples.EntityFrameworkCore.Scenarios.S03SoftDelete;

internal sealed class Runner : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("S03", "Soft Delete — Focused SoftDeleteInterceptor Demo");

        var services = new ServiceCollection();

        services.AddDbContext<InvoiceDbContext>((sp, o) =>
            o.UseInMemoryDatabase("S03SoftDelete")
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        // Only SoftDeleteInterceptor — no auditable interceptor registered here.
        // This means Deleted/DeletedBy audit fields will remain null/default after a soft-delete.
        services.AddSoftDeleteInterceptor();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        await db.Database.EnsureCreatedAsync();

        // ── Step 1: Insert invoices ──────────────────────────────────────────
        Print.Step("1", "Insert three invoices");

        db.Invoices.AddRange(
            new Invoice
            {
                Number = "S03-001", Description = "Alpha", Amount = 100, Date = new DateOnly(2025, 1, 1), IsPaid = false
            },
            new Invoice
            {
                Number = "S03-002", Description = "Beta", Amount = 200, Date = new DateOnly(2025, 1, 2), IsPaid = false
            },
            new Invoice
            {
                Number = "S03-003", Description = "Gamma", Amount = 300, Date = new DateOnly(2025, 1, 3), IsPaid = true
            }
        );
        await db.SaveChangesAsync();

        Console.WriteLine($"  Inserted 3 invoices. Active count: {await db.Invoices.CountAsync()}");

        // ── Step 2: Soft-delete via db.Remove() ──────────────────────────────
        Print.Step("2", "Soft-delete S03-002 via db.Remove() — interceptor converts to IsDeleted=true");

        var toRemove = await db.Invoices.FirstAsync(i => i.Number == "S03-002");
        db.Remove(toRemove);
        await db.SaveChangesAsync();

        var softDeleted = await db.Invoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(i => i.Number == "S03-002");

        Console.WriteLine($"  🗑️  {softDeleted.Number} | IsDeleted: {softDeleted.IsDeleted}");
        Console.WriteLine(
            $"     Deleted (audit): {softDeleted.Deleted?.ToString("u") ?? "null — no AuditableEntityInterceptor registered"}");
        Console.WriteLine(
            $"     DeletedBy (audit): {softDeleted.DeletedBy?.ToString() ?? "null — no AuditableEntityInterceptor registered"}");

        // ── Step 3: Global query filter hides soft-deleted records ────────────
        Print.Step("3", "Global query filter — soft-deleted record is excluded from normal queries");

        var activeCount = await db.Invoices.CountAsync();
        var totalCount = await db.Invoices.IgnoreQueryFilters().CountAsync();

        Console.WriteLine($"  Active  (with filter):    {activeCount}");
        Console.WriteLine($"  Total   (IgnoreQueryFilters): {totalCount}");

        foreach (var inv in db.Invoices.IgnoreQueryFilters().AsNoTracking().OrderBy(i => i.Number))
            Console.WriteLine($"    • {inv.Number} | IsDeleted: {inv.IsDeleted}");

        // ── Step 4: Manual IsDeleted = true path ─────────────────────────────
        Print.Step("4", "Manual IsDeleted=true — direct property change, no db.Remove() needed");

        var manual = await db.Invoices.FirstAsync(i => i.Number == "S03-003");
        manual.IsDeleted = true;
        await db.SaveChangesAsync();

        var manualRecord = await db.Invoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(i => i.Number == "S03-003");

        Console.WriteLine(
            $"  ✏️  {manualRecord.Number} | IsDeleted: {manualRecord.IsDeleted}  (set directly on entity, no interceptor involved)");

        var finalActive = await db.Invoices.CountAsync();
        Console.WriteLine($"  📊 Final active count: {finalActive}  (both S03-002 and S03-003 are soft-deleted)");
    }
}
