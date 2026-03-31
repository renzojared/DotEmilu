using DotEmilu.EntityFrameworkCore;
using DotEmilu.Samples.Domain.Entities;
using DotEmilu.Samples.EntityFrameworkCore.DataAccess;
using DotEmilu.Samples.EntityFrameworkCore.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.Samples.EntityFrameworkCore.Scenarios.S01AuditableExplicit;

internal sealed class Runner : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("S01", "Auditable Entity — Explicit Registration");

        var services = new ServiceCollection();

        services.AddDbContext<InvoiceDbContext>((sp, o) =>
            o.UseInMemoryDatabase("S01AuditableExplicit")
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        services.AddSoftDeleteInterceptor();
        services.AddAuditableEntityInterceptor<MockContextUser, Guid>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        await db.Database.EnsureCreatedAsync();

        // ── Step 0: Interceptor order from DI ─────────────────────────────────
        Print.Step("0", "DI registration order — SoftDeleteInterceptor before AuditableEntityInterceptor<Guid>");
        var interceptorOrder = scope.ServiceProvider
            .GetServices<ISaveChangesInterceptor>()
            .Select(i => i.GetType().Name);
        Console.WriteLine($"  Interceptors resolved by DI: {string.Join(" -> ", interceptorOrder)}");

        // ── Step 1: Insert ─────────────────────────────────────────────────────
        Print.Step("1", "Insert invoices — Created / CreatedBy auto-populated by interceptor");

        db.Invoices.AddRange(
            new Invoice
            {
                Number = "INV-001", Description = "Web development", Amount = 1500, Date = new DateOnly(2025, 1, 15),
                IsPaid = true
            },
            new Invoice
            {
                Number = "INV-002", Description = "Consulting", Amount = 3000, Date = new DateOnly(2025, 2, 20),
                IsPaid = false
            },
            new Invoice
            {
                Number = "INV-003", Description = "Maintenance", Amount = 800, Date = new DateOnly(2025, 3, 10),
                IsPaid = false
            }
        );
        await db.SaveChangesAsync();

        foreach (var inv in db.Invoices.AsNoTracking().OrderBy(i => i.Id))
            Console.WriteLine($"  📄 {inv.Number} | Created: {inv.Created:u} | CreatedBy: {inv.CreatedBy}");

        // ── Step 2: Update ─────────────────────────────────────────────────────
        Print.Step("2", "Update INV-001 — LastModified / LastModifiedBy change");

        var toUpdate = await db.Invoices.FirstAsync(i => i.Number == "INV-001");
        toUpdate.Description = "Web development (revised)";
        await db.SaveChangesAsync();

        var updated = await db.Invoices.AsNoTracking().FirstAsync(i => i.Number == "INV-001");
        Console.WriteLine(
            $"  ✏️  {updated.Number} | LastModified: {updated.LastModified:u} | LastModifiedBy: {updated.LastModifiedBy}");

        // ── Step 3: Soft-delete via db.Remove() ────────────────────────────────
        Print.Step("3", "Soft-delete INV-002 via db.Remove() — interceptor fills Deleted / DeletedBy");

        var toDelete = await db.Invoices.FirstAsync(i => i.Number == "INV-002");
        db.Remove(toDelete);
        await db.SaveChangesAsync();

        var softDeleted = await db.Invoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(i => i.Number == "INV-002");

        Console.WriteLine(
            $"  🗑️  {softDeleted.Number} | IsDeleted: {softDeleted.IsDeleted} | Deleted: {softDeleted.Deleted:u} | DeletedBy: {softDeleted.DeletedBy}");

        var active = await db.Invoices.CountAsync();
        var total = await db.Invoices.IgnoreQueryFilters().CountAsync();
        Console.WriteLine($"  📊 Active (filtered): {active}  |  Total (unfiltered): {total}");
    }
}
