using DotEmilu.EntityFrameworkCore;
using DotEmilu.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.IntegrationTests.EntityFrameworkCore;

public sealed class AuditableInterceptorTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private TestDbContext _db = null!;
    private MutableTimeProvider _clock = null!;

    public async ValueTask InitializeAsync()
    {
        _clock = new MutableTimeProvider(new DateTimeOffset(2025, 01, 01, 00, 00, 00, TimeSpan.Zero));
        var services = new ServiceCollection();

        services.AddSingleton(_clock);
        services.AddSingleton<TimeProvider>(sp => sp.GetRequiredService<MutableTimeProvider>());
        services.AddSoftDeleteInterceptor();
        services.AddAuditableEntityInterceptor<TestContextUser, Guid>();
        services.AddDbContext<TestDbContext>((sp, options) =>
            options
                .UseInMemoryDatabase($"Auditable_{Guid.NewGuid()}")
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        _provider = services.BuildServiceProvider();
        _db = _provider.GetRequiredService<TestDbContext>();
        await _db.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task AddedEntity_WhenSaved_ThenSetsCreatedAndCreatedBy()
    {
        var expectedUtcNow = _clock.GetUtcNow();
        var entity = new TestAuditableEntity { Title = "New" };
        _db.TestAuditableEntities.Add(entity);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expectedUtcNow, entity.Created);
        Assert.Equal(TestContextUser.FixedUserId, entity.CreatedBy);
        Assert.Equal(expectedUtcNow, entity.LastModified);
        Assert.Equal(TestContextUser.FixedUserId, entity.LastModifiedBy);
    }

    [Fact]
    public async Task ModifiedEntity_WhenSaved_ThenUpdatesLastModifiedAndLastModifiedBy()
    {
        var entity = new TestAuditableEntity { Title = "Original" };
        _db.TestAuditableEntities.Add(entity);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var originalLastModified = entity.LastModified;

        _clock.Advance(TimeSpan.FromMinutes(1));
        entity.Title = "Updated";
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.True(entity.LastModified > originalLastModified);
        Assert.Equal(_clock.GetUtcNow(), entity.LastModified);
        Assert.Equal(TestContextUser.FixedUserId, entity.LastModifiedBy);
    }

    [Fact]
    public async Task SoftDelete_WhenIsDeletedSetTrue_ThenSetsDeletedAndDeletedBy()
    {
        var entity = new TestAuditableEntity { Title = "ToSoftDelete" };
        _db.TestAuditableEntities.Add(entity);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _clock.Advance(TimeSpan.FromMinutes(1));
        entity.IsDeleted = true;
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var deleted = await _db.TestAuditableEntities.IgnoreQueryFilters()
            .FirstAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);

        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.Deleted);
        Assert.Equal(_clock.GetUtcNow(), deleted.Deleted);
        Assert.Equal(TestContextUser.FixedUserId, deleted.DeletedBy);
    }

    [Fact]
    public async Task HardDelete_WhenRemoved_ThenConvertsToSoftDelete()
    {
        var entity = new TestAuditableEntity { Title = "ToHardDelete" };
        _db.TestAuditableEntities.Add(entity);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _clock.Advance(TimeSpan.FromMinutes(1));
        _db.TestAuditableEntities.Remove(entity);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var deleted = await _db.TestAuditableEntities.IgnoreQueryFilters()
            .FirstAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);

        Assert.True(deleted.IsDeleted);
        Assert.Equal(_clock.GetUtcNow(), deleted.Deleted);
        Assert.Equal(TestContextUser.FixedUserId, deleted.DeletedBy);
    }

    [Fact]
    public async Task OwnedEntity_WhenOwnedMemberChanges_ThenUpdatesLastModifiedEvenIfOwnerUnchanged()
    {
        var ownedClock = new MutableTimeProvider(new DateTimeOffset(2025, 01, 01, 00, 00, 00, TimeSpan.Zero));
        var services = new ServiceCollection();
        services.AddSingleton(ownedClock);
        services.AddSingleton<TimeProvider>(sp => sp.GetRequiredService<MutableTimeProvider>());
        services.AddAuditableEntityInterceptor<TestContextUser, Guid>();
        services.AddDbContext<OwnedAuditableDbContext>((sp, options) =>
            options
                .UseInMemoryDatabase($"AuditableOwned_{Guid.NewGuid()}")
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OwnedAuditableDbContext>();
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var entity = new OwnedAuditableEntity
        {
            Title = "Owned",
            Metadata = new OwnedMetadata { Note = "v1" }
        };
        db.Entities.Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var firstLastModified = entity.LastModified;

        ownedClock.Advance(TimeSpan.FromMinutes(1));
        entity.Metadata.Note = "v2";

        var ownerEntry = db.Entry(entity);
        ownerEntry.State = EntityState.Unchanged;
        var ownedEntry = ownerEntry.Reference(e => e.Metadata).TargetEntry!;
        ownedEntry.State = EntityState.Modified;

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.True(entity.LastModified > firstLastModified);
        Assert.Equal(ownedClock.GetUtcNow(), entity.LastModified);
        Assert.Equal(TestContextUser.FixedUserId, entity.LastModifiedBy);
    }

    private sealed class OwnedAuditableDbContext(DbContextOptions<OwnedAuditableDbContext> options)
        : DbContext(options)
    {
        public DbSet<OwnedAuditableEntity> Entities => Set<OwnedAuditableEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OwnedAuditableEntity>(builder =>
            {
                new BaseAuditableEntityConfiguration<OwnedAuditableEntity, Guid>(MappingStrategy.Tph)
                    .Configure(builder);
                builder.HasKey(x => x.Id);
                builder.OwnsOne(x => x.Metadata);
            });
        }
    }

    private sealed class OwnedAuditableEntity : BaseAuditableEntity<Guid, Guid>
    {
        public string Title { get; set; } = string.Empty;
        public OwnedMetadata Metadata { get; set; } = new();
    }

    [Owned]
    private sealed class OwnedMetadata
    {
        public string Note { get; set; } = string.Empty;
    }

    private sealed class MutableTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = initialUtcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        internal void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }
}
