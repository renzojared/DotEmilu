using DotEmilu.EntityFrameworkCore;
using DotEmilu.Samples.EntityFrameworkCore.DataAccess;
using DotEmilu.Samples.Domain.Entities;
using DotEmilu.Samples.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.Samples.EntityFrameworkCore.Scenarios.S06ModelConfiguration;

internal sealed class Runner : IScenario
{
    public async Task RunAsync()
    {
        Print.Header("S06", "Model Configuration — UseIsDeleted + HasFormattedComment + MappingStrategy notes");

        var services = new ServiceCollection();

        services.AddDbContext<InvoiceDbContext>((sp, o) =>
            o.UseInMemoryDatabase("S06ModelConfiguration")
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        services.AddSoftDeleteInterceptor();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        await db.Database.EnsureCreatedAsync();

        // ── Step 1: Add songs of different SongType values ───────────────────
        Print.Step("1",
            "Add songs — Type stored as int enum, IsDeleted stored as short (0/1) via UseIsDeleted(useShort: true)");

        db.Songs.AddRange(
            new Song { Name = "Vivir Mi Vida", Type = SongType.Salsa },
            new Song { Name = "Bohemian Rhapsody", Type = SongType.Rock },
            new Song { Name = "Take Five", Type = SongType.Jazz }
        );
        await db.SaveChangesAsync();

        foreach (var song in db.Songs.AsNoTracking().OrderBy(s => s.Name))
            Console.WriteLine(
                $"  🎵 {song.Name,-25} | Type: {song.Type,-5} ({(int)song.Type}) | IsDeleted: {song.IsDeleted}");

        // ── Step 2: HasFormattedComment on SongType ───────────────────────────
        Print.Step("2", "HasFormattedComment(\"{0} = {2}\", includeTitle: true) on SongType property");

        Console.WriteLine(
            "  Format '{0} = {2}' uses: {0}=numeric value, {2}=Description attribute (fallback to enum name)");
        Console.WriteLine("  SongType has [Description(\"Song genres\")] at the type level.");

        // Column comments live in EF Core's model metadata and ARE accessible at runtime — even with
        // the InMemory provider. In EF Core 8+, DbContext.Model returns a read-optimized model that
        // strips design-time annotations, so we retrieve the full model via IDesignTimeModel.
        // For real SQL providers the same comments are also embedded in the migration DDL.
        var fullModel = db.GetService<IDesignTimeModel>().Model;
        var songEntityType = fullModel.FindEntityType(typeof(Song))!;
        var actualComment = songEntityType.FindProperty(nameof(Song.Type))!.GetComment();
        const string expectedComment = "Song genres: 0 = Salsa, 1 = Rock & Roll, 2 = Jazz";

        Console.WriteLine($"  Actual comment from EF model : \"{actualComment}\"");
        Console.WriteLine($"  Expected                     : \"{expectedComment}\"");
        Console.WriteLine($"  Comment matches              : {actualComment == expectedComment}");

        // ── Step 3: MappingStrategy signal vs observable schema behavior ─────
        Print.Step("3",
            "MappingStrategy metadata exists, but no inheritance hierarchy means no discriminator/derived mapping");

        var invoiceEntityType = fullModel.FindEntityType(typeof(Invoice))!;
        var songMappingStrategy =
            songEntityType.FindAnnotation("Relational:MappingStrategy")?.Value?.ToString() ?? "(none)";
        var invoiceMappingStrategy =
            invoiceEntityType.FindAnnotation("Relational:MappingStrategy")?.Value?.ToString() ?? "(none)";
        var songHasDerivedTypes = songEntityType.GetDirectlyDerivedTypes().Any();
        var invoiceHasDerivedTypes = invoiceEntityType.GetDirectlyDerivedTypes().Any();
        var songDiscriminator = songEntityType.FindDiscriminatorProperty()?.Name ?? "(none)";
        var invoiceDiscriminator = invoiceEntityType.FindDiscriminatorProperty()?.Name ?? "(none)";

        Console.WriteLine($"  Song mapping strategy annotation    : {songMappingStrategy}");
        Console.WriteLine($"  Invoice mapping strategy annotation : {invoiceMappingStrategy}");
        Console.WriteLine($"  Song has derived types              : {songHasDerivedTypes}");
        Console.WriteLine($"  Invoice has derived types           : {invoiceHasDerivedTypes}");
        Console.WriteLine($"  Song discriminator                  : {songDiscriminator}");
        Console.WriteLine($"  Invoice discriminator               : {invoiceDiscriminator}");
        Console.WriteLine("  ℹ️  Without a concrete inheritance hierarchy, strategy is intent metadata only.");

        // ── Step 4: Soft-delete via db.Remove() ──────────────────────────────
        Print.Step("4",
            "Soft-delete 'Take Five' via db.Remove() — IsDeleted stored as short 1, global filter excludes it");

        var toDelete = await db.Songs.FirstAsync(s => s.Name == "Take Five");
        db.Remove(toDelete);
        await db.SaveChangesAsync();

        var deletedSong = await db.Songs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(s => s.Name == "Take Five");

        Console.WriteLine(
            $"  🗑️  '{deletedSong.Name}' | IsDeleted: {deletedSong.IsDeleted}  (short in DB: {(deletedSong.IsDeleted ? 1 : 0)})");

        var active = await db.Songs.CountAsync();
        var total = await db.Songs.IgnoreQueryFilters().CountAsync();
        Console.WriteLine($"  📊 Active (filtered): {active}  |  Total (IgnoreQueryFilters): {total}");

        // ── Step 5: Remaining songs still visible via default query ──────────
        Print.Step("5", "Default query respects global filter — only IsDeleted=false (short 0) rows returned");

        foreach (var song in db.Songs.AsNoTracking().OrderBy(s => s.Name))
            Console.WriteLine(
                $"  🎵 {song.Name,-25} | Type: {song.Type} | IsDeleted: {song.IsDeleted} (short: {(song.IsDeleted ? 1 : 0)})");
    }
}
