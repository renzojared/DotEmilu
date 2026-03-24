using DotEmilu.EntityFrameworkCore.Extensions;
using DotEmilu.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.IntegrationTests.EntityFrameworkCore;

public sealed class QueryExtensionsTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private TestDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase($"QueryExt_{Guid.NewGuid()}"));

        _provider = services.BuildServiceProvider();
        _db = _provider.GetRequiredService<TestDbContext>();
        await _db.Database.EnsureCreatedAsync();

        _db.TestEntities.AddRange(
            new TestEntity { Name = "A" },
            new TestEntity { Name = "B" },
            new TestEntity { Name = "C" },
            new TestEntity { Name = "D" },
            new TestEntity { Name = "E" }
        );
        await _db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task AsPaginatedListAsync_WhenCalled_ThenReturnsCorrectPage()
    {
        var result = await _db.TestEntities
            .OrderBy(e => e.Name)
            .AsPaginatedListAsync(pageNumber: 1, pageSize: 2, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task AsPaginatedListAsync_WhenLastPage_ThenHasNextPageIsFalse()
    {
        var result = await _db.TestEntities
            .OrderBy(e => e.Name)
            .AsPaginatedListAsync(pageNumber: 3, pageSize: 2, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.False(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public async Task AsPaginatedListAsync_WhenPageSizeIsOne_ThenReturnsSingleItemAndCorrectTotalPages()
    {
        var result = await _db.TestEntities
            .OrderBy(e => e.Name)
            .AsPaginatedListAsync(pageNumber: 1, pageSize: 1, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal(5, result.TotalPages);
    }

    [Fact]
    public async Task AsPaginatedListAsync_WhenNoResults_ThenReturnsEmptyWithZeroTotalPages()
    {
        var emptyDb = _provider.GetRequiredService<TestDbContext>();

        var result = await emptyDb.TestEntities
            .IgnoreQueryFilters()
            .Where(e => e.Name == "__nonexistent__")
            .AsPaginatedListAsync(pageNumber: 1, pageSize: 10, TestContext.Current.CancellationToken);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task AsPaginatedListAsync_WhenPageNumberExceedsTotalPages_ThenReturnsEmptyItems()
    {
        var result = await _db.TestEntities
            .OrderBy(e => e.Name)
            .AsPaginatedListAsync(pageNumber: 999, pageSize: 10, TestContext.Current.CancellationToken);

        Assert.Empty(result.Items);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task AsPaginatedListAsync_WhenIgnoreQueryFilters_ThenIncludesSoftDeletedInTotalCount()
    {
        _db.TestEntities.Add(new TestEntity { Name = "Deleted", IsDeleted = true });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var filtered = await _db.TestEntities
            .OrderBy(e => e.Name)
            .AsPaginatedListAsync(pageNumber: 1, pageSize: 10, TestContext.Current.CancellationToken);

        var unfiltered = await _db.TestEntities
            .IgnoreQueryFilters()
            .OrderBy(e => e.Name)
            .AsPaginatedListAsync(pageNumber: 1, pageSize: 10, TestContext.Current.CancellationToken);

        Assert.Equal(5, filtered.TotalCount);
        Assert.Equal(6, unfiltered.TotalCount);
    }
}
