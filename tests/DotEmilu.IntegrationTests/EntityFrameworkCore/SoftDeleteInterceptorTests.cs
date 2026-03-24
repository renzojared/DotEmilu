using DotEmilu.EntityFrameworkCore;
using DotEmilu.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.IntegrationTests.EntityFrameworkCore;

public sealed class SoftDeleteInterceptorTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private TestDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddSoftDeleteInterceptor();
        services.AddDbContext<TestDbContext>((sp, options) =>
            options
                .UseInMemoryDatabase($"SoftDelete_{Guid.NewGuid()}")
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
    public async Task Remove_WhenCalled_ThenSetsIsDeletedAndKeepsEntityStateUnchanged()
    {
        var entity = new TestEntity { Name = "ToDelete" };
        _db.TestEntities.Add(entity);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _db.TestEntities.Remove(entity);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var deleted = await _db.TestEntities.IgnoreQueryFilters()
            .FirstAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);
        Assert.True(deleted.IsDeleted);
        Assert.Equal(EntityState.Unchanged, _db.Entry(entity).State);
    }

    [Fact]
    public async Task GlobalQueryFilter_WhenEntityIsSoftDeleted_ThenExcludesFromResults()
    {
        var entity = new TestEntity { Name = "Filtered" };
        _db.TestEntities.Add(entity);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _db.TestEntities.Remove(entity);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var activeCount = await _db.TestEntities.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, activeCount);
    }

    [Fact]
    public async Task IgnoreQueryFilters_WhenCalled_ThenReturnsSoftDeletedRecords()
    {
        var entity = new TestEntity { Name = "Visible" };
        _db.TestEntities.Add(entity);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _db.TestEntities.Remove(entity);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var totalCount = await _db.TestEntities.IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, totalCount);
    }
}
