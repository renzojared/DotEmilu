using DotEmilu.EntityFrameworkCore;
using DotEmilu.EntityFrameworkCore.Extensions;
using DotEmilu.Samples.Domain.Entities;
using DotEmilu.Samples.EntityFrameworkCore.Entities;
using DotEmilu.Samples.EntityFrameworkCore.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.Samples.EntityFrameworkCore.Scenarios.S07ModelBuilderOverloads;

internal sealed class Runner : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("S07", "ModelBuilder Generic Overloads — ApplyBase*Configuration<T>");

        var services = new ServiceCollection();

        services
            .AddSoftDeleteInterceptor()
            .AddAuditableEntityInterceptor<MockContextUser, Guid>();

        services.AddDbContext<GenericOverloadsDbContext>((sp, o) =>
            o.UseInMemoryDatabase("S07ModelBuilderOverloads")
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<GenericOverloadsDbContext>();
        await db.Database.EnsureCreatedAsync();

        // ── Step 1: Generic overloads configured in OnModelCreating ───────────
        Print.Step("1", "Use generic overloads (no assembly scanning)");
        Console.WriteLine("  ApplyBaseAuditableEntityConfiguration<Guid>(Tph)");
        Console.WriteLine("  ApplyBaseEntityConfiguration<int>(Tpt)");
        Console.WriteLine("  ApplyBaseEntityConfiguration<Guid>(Tpc)");

        // ── Step 2: Insert and verify auditable data ──────────────────────────
        Print.Step("2", "Insert Invoice + Song and save changes");

        db.Invoices.Add(new Invoice
        {
            Number = "S07-INV-001",
            Description = "Generic overload demo",
            Amount = 1200,
            Date = new DateOnly(2025, 7, 1),
            IsPaid = false
        });
        db.Songs.Add(new Song { Name = "S07-Song-01", Type = SongType.Jazz });
        await db.SaveChangesAsync();

        var invoice = await db.Invoices.AsNoTracking().FirstAsync(i => i.Number == "S07-INV-001");
        Console.WriteLine($"  📄 {invoice.Number} | Created: {invoice.Created:u} | CreatedBy: {invoice.CreatedBy}");

        // ── Step 3: Soft-delete both entities ─────────────────────────────────
        Print.Step("3", "db.Remove() uses soft-delete; auditable delete fields filled for Invoice");

        var invoiceToDelete = await db.Invoices.FirstAsync(i => i.Number == "S07-INV-001");
        var songToDelete = await db.Songs.FirstAsync(s => s.Name == "S07-Song-01");
        db.Remove(invoiceToDelete);
        db.Remove(songToDelete);
        await db.SaveChangesAsync();

        var deletedInvoice = await db.Invoices.IgnoreQueryFilters().AsNoTracking().FirstAsync(i => i.Number == "S07-INV-001");
        var deletedSong = await db.Songs.IgnoreQueryFilters().AsNoTracking().FirstAsync(s => s.Name == "S07-Song-01");

        Console.WriteLine(
            $"  🧾 Invoice | IsDeleted: {deletedInvoice.IsDeleted} | Deleted: {deletedInvoice.Deleted:u} | DeletedBy: {deletedInvoice.DeletedBy}");
        Console.WriteLine($"  🎵 Song    | IsDeleted: {deletedSong.IsDeleted}");

        // ── Step 4: Query filter check ─────────────────────────────────────────
        Print.Step("4", "Global filter still applies with generic overload setup");
        Console.WriteLine($"  Active invoices: {await db.Invoices.CountAsync()} | Total: {await db.Invoices.IgnoreQueryFilters().CountAsync()}");
        Console.WriteLine($"  Active songs   : {await db.Songs.CountAsync()} | Total: {await db.Songs.IgnoreQueryFilters().CountAsync()}");
    }
}

internal sealed class GenericOverloadsDbContext(DbContextOptions<GenericOverloadsDbContext> options)
    : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Song> Songs => Set<Song>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Invoice>();
        modelBuilder.Entity<Song>();

        modelBuilder
            .ApplyBaseAuditableEntityConfiguration<Guid>(MappingStrategy.Tph)
            .ApplyBaseEntityConfiguration<int>(MappingStrategy.Tpt, enableRowVersion: false)
            .ApplyBaseEntityConfiguration<Guid>();
    }
}
