namespace DotEmilu.Abstractions;

/// <summary>
/// Represents a paginated collection of items.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class PaginatedList<T>
{
    /// <summary>Gets the items on the current page.</summary>
    public IReadOnlyCollection<T> Items { get; }

    /// <summary>Gets the current page number (1-based).</summary>
    public int PageNumber { get; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages { get; }

    /// <summary>Gets the total count of items across all pages.</summary>
    public int TotalCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginatedList{T}"/> class.
    /// </summary>
    /// <param name="items">The items on the current page.</param>
    /// <param name="count">The total number of items.</param>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    public PaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }

    /// <summary>Gets a value indicating whether there is a previous page.</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>Gets a value indicating whether there is a next page.</summary>
    public bool HasNextPage => PageNumber < TotalPages;
}