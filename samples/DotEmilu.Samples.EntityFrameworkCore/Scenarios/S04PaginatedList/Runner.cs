using DotEmilu.EntityFrameworkCore;
using DotEmilu.EntityFrameworkCore.Extensions;
using DotEmilu.Samples.EntityFrameworkCore.DataAccess;
using DotEmilu.Samples.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.Samples.EntityFrameworkCore.Scenarios.S04PaginatedList;

internal sealed class Runner : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("S04", "Paginated List — AsPaginatedListAsync");

        var services = new ServiceCollection();

        services.AddDbContext<InvoiceDbContext>((sp, o) =>
            o.UseInMemoryDatabase("S04PaginatedList")
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        services.AddSoftDeleteInterceptor();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        await db.Database.EnsureCreatedAsync();

        // ── Seed: 10 active songs + 1 that will be soft-deleted ──────────────
        db.Songs.AddRange(
            new Song { Name = "Song 01", Type = SongType.Salsa },
            new Song { Name = "Song 02", Type = SongType.Rock },
            new Song { Name = "Song 03", Type = SongType.Jazz },
            new Song { Name = "Song 04", Type = SongType.Salsa },
            new Song { Name = "Song 05", Type = SongType.Rock },
            new Song { Name = "Song 06", Type = SongType.Jazz },
            new Song { Name = "Song 07", Type = SongType.Salsa },
            new Song { Name = "Song 08", Type = SongType.Rock },
            new Song { Name = "Song 09", Type = SongType.Jazz },
            new Song { Name = "Song 10", Type = SongType.Salsa },
            new Song { Name = "Song 11 (to be deleted)", Type = SongType.Rock }
        );
        await db.SaveChangesAsync();

        // Soft-delete Song 11 so pagination only sees 10 active records
        var toDelete = await db.Songs.FirstAsync(s => s.Name == "Song 11 (to be deleted)");
        db.Remove(toDelete);
        await db.SaveChangesAsync();

        // ── Step 1: Page 1 (size 3) ──────────────────────────────────────────
        Print.Step("1", "Page 1, size 3 — global filter hides the soft-deleted song (10 total active)");

        var page1 = await db.Songs
            .OrderBy(s => s.Name)
            .AsPaginatedListAsync(pageNumber: 1, pageSize: 3, CancellationToken.None);

        Console.WriteLine($"  PageNumber    : {page1.PageNumber}");
        Console.WriteLine($"  TotalCount    : {page1.TotalCount}   ← soft-deleted song excluded");
        Console.WriteLine($"  TotalPages    : {page1.TotalPages}");
        Console.WriteLine($"  HasPreviousPage: {page1.HasPreviousPage}");
        Console.WriteLine($"  HasNextPage   : {page1.HasNextPage}");

        foreach (var s in page1.Items)
            Console.WriteLine($"    🎵 {s.Name}");

        // ── Step 2: Page 2 ───────────────────────────────────────────────────
        Print.Step("2", "Page 2, size 3 — different slice of results");

        var page2 = await db.Songs
            .OrderBy(s => s.Name)
            .AsPaginatedListAsync(pageNumber: 2, pageSize: 3, CancellationToken.None);

        Console.WriteLine($"  PageNumber    : {page2.PageNumber}");
        Console.WriteLine($"  HasPreviousPage: {page2.HasPreviousPage}");
        Console.WriteLine($"  HasNextPage   : {page2.HasNextPage}");

        foreach (var s in page2.Items)
            Console.WriteLine($"    🎵 {s.Name}");

        // ── Step 3: Last page ────────────────────────────────────────────────
        Print.Step("3", "Last page (4 of 4) — partial page, HasNextPage = false");

        var lastPage = await db.Songs
            .OrderBy(s => s.Name)
            .AsPaginatedListAsync(pageNumber: 4, pageSize: 3, CancellationToken.None);

        Console.WriteLine($"  PageNumber    : {lastPage.PageNumber}");
        Console.WriteLine($"  HasPreviousPage: {lastPage.HasPreviousPage}");
        Console.WriteLine($"  HasNextPage   : {lastPage.HasNextPage}");

        foreach (var s in lastPage.Items)
            Console.WriteLine($"    🎵 {s.Name}");

        // ── Step 4: IgnoreQueryFilters — deleted song appears in total ────────
        Print.Step("4", "IgnoreQueryFilters — soft-deleted song included in TotalCount");

        var unfiltered = await db.Songs
            .IgnoreQueryFilters()
            .OrderBy(s => s.Name)
            .AsPaginatedListAsync(pageNumber: 1, pageSize: 3, CancellationToken.None);

        Console.WriteLine($"  TotalCount (unfiltered): {unfiltered.TotalCount}  ← 10 active + 1 soft-deleted");
    }
}
