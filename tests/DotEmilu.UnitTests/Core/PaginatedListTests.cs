namespace DotEmilu.UnitTests.Core;

public class PaginatedListTests
{
    [Theory]
    [InlineData(1, 5, 10, false, true, 2)] // first page
    [InlineData(2, 5, 10, true, false, 2)] // last page (exact fit)
    [InlineData(2, 3, 7, true, true, 3)] // middle page
    [InlineData(3, 3, 7, true, false, 3)] // last page (ceiling)
    [InlineData(1, 5, 5, false, false, 1)] // single page
    [InlineData(1, 5, 0, false, false, 0)] // empty list
    public void Navigation_WhenPageAndSizeSet_ThenHasPreviousNextAndTotalPagesAreCorrect(
        int pageNumber, int pageSize, int totalCount,
        bool expectedHasPrevious, bool expectedHasNext, int expectedTotalPages)
    {
        var sut = new PaginatedList<int>([], count: totalCount, pageNumber: pageNumber, pageSize: pageSize);

        Assert.Equal(expectedHasPrevious, sut.HasPreviousPage);
        Assert.Equal(expectedHasNext, sut.HasNextPage);
        Assert.Equal(expectedTotalPages, sut.TotalPages);
    }

    [Fact]
    public void Constructor_WhenCalled_ThenSetsItemsAndMetadataCorrectly()
    {
        var items = new List<string> { "a", "b" }.AsReadOnly();

        var sut = new PaginatedList<string>(items, count: 5, pageNumber: 2, pageSize: 2);

        Assert.Equal(items, sut.Items);
        Assert.Equal(2, sut.PageNumber);
        Assert.Equal(5, sut.TotalCount);
        Assert.Equal(3, sut.TotalPages);
    }
}

