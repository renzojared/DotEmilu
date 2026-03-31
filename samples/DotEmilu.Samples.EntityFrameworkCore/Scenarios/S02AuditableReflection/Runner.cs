using System.Reflection;
using DotEmilu.EntityFrameworkCore;
using DotEmilu.Samples.Domain.Entities;
using DotEmilu.Samples.EntityFrameworkCore.DataAccess;
using DotEmilu.Samples.EntityFrameworkCore.Entities;
using DotEmilu.Samples.EntityFrameworkCore.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.Samples.EntityFrameworkCore.Scenarios.S02AuditableReflection;

internal sealed class Runner : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("S02", "Auditable Entity — Reflection-based Discovery");

        var services = new ServiceCollection();

        services.AddDbContext<InvoiceDbContext>((sp, o) =>
            o.UseInMemoryDatabase("S02AuditableReflection")
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        services.AddSoftDeleteInterceptor();

        // Scans the assembly and registers one AuditableEntityInterceptor per discovered IContextUser<T>:
        //   • MockContextUser : IAppUserContext : IContextUser<Guid>  →  AuditableEntityInterceptor<Guid>
        //   • SystemUser      : IContextUser<int>                     →  AuditableEntityInterceptor<int>
        // Note: MockContextUser implements IContextUser<Guid> indirectly through the domain contract
        // IAppUserContext. The scanner resolves it via GetInterfaces(), which traverses the full hierarchy.
        services.AddAuditableEntityInterceptors(Assembly.GetExecutingAssembly());

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        await db.Database.EnsureCreatedAsync();

        // ── Step 1 ──────────────────────────────────────────────────────────
        Print.Step("1", "Song (BaseEntity<Guid>) — NOT auditable, interceptor ignores it");

        db.Songs.Add(new Song { Name = "Vivir Mi Vida", Type = SongType.Salsa });
        await db.SaveChangesAsync();

        var song = await db.Songs.AsNoTracking().FirstAsync();
        Console.WriteLine($"  🎵 Song '{song.Name}' | IsDeleted: {song.IsDeleted}");
        Console.WriteLine("     (Song has no Created/CreatedBy — BaseEntity<Guid> is not auditable)");

        // ── Step 2 ──────────────────────────────────────────────────────────
        Print.Step("2", "Invoice (BaseAuditableEntity<int, Guid>) — MockContextUser<Guid> populates audit fields");

        db.Invoices.Add(new Invoice
        {
            Number = "INV-R01",
            Description = "Reflection-discovered auditing demo",
            Amount = 999,
            Date = new DateOnly(2025, 6, 1),
            IsPaid = false
        });
        await db.SaveChangesAsync();

        var inv = await db.Invoices.AsNoTracking().FirstAsync(i => i.Number == "INV-R01");
        var mockUser = new MockContextUser();

        Console.WriteLine($"  📄 {inv.Number} | Created: {inv.Created:u} | CreatedBy: {inv.CreatedBy}");
        Console.WriteLine($"     MockContextUser.Id : {mockUser.Id}");
        Console.WriteLine($"     CreatedBy matches  : {inv.CreatedBy == mockUser.Id}");

        // ── Step 3 ──────────────────────────────────────────────────────────
        Print.Step("3",
            "SystemUser (IContextUser<int>) discovered — no int-keyed auditable entities in this DbContext");

        Console.WriteLine($"  ℹ️  SystemUser.Id = {new SystemUser().Id}");
        Console.WriteLine("     AuditableEntityInterceptor<int> is registered but finds no IBaseAuditableEntity<int>.");
        Console.WriteLine("     Reflection-based discovery is equivalent to calling");
        Console.WriteLine("     AddAuditableEntityInterceptor<MockContextUser, Guid>() +");
        Console.WriteLine("     AddAuditableEntityInterceptor<SystemUser, int>() explicitly.");
    }
}
