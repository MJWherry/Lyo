namespace Lyo.Common;

/// <summary>
/// Represents a single page of results from a paged query.
/// Use as the data payload of <see cref="Result{T}"/>: <c>Result&lt;PagedResult&lt;T&gt;&gt;</c>.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public record PagedResult<T>
{
    /// <summary>The items on this page.</summary>
    public IReadOnlyList<T> Items { get; init; }

    /// <summary>Total number of items across all pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>1-based current page number.</summary>
    public int Page { get; init; }

    /// <summary>Maximum number of items per page.</summary>
    public int PageSize { get; init; }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Whether a next page exists.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Whether a previous page exists.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Whether this page contains no items.</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>Whether this is the first page.</summary>
    public bool IsFirstPage => Page == 1;

    /// <summary>Whether this is the last page.</summary>
    public bool IsLastPage => Page >= TotalPages;

    /// <summary>The 0-based index of the first item on this page relative to the full result set.</summary>
    public int Offset => (Page - 1) * PageSize;

    /// <summary>Creates an empty paged result with no items and a total count of zero.</summary>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 20)
        => new([], 0, page, pageSize);

    /// <summary>Creates a paged result treating the provided list as a complete single-page result set.</summary>
    public static PagedResult<T> SinglePage(IReadOnlyList<T> items, int pageSize = 20)
        => new(items, items.Count, 1, pageSize);

    /// <summary>Projects each item to a new type, preserving paging metadata.</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> mapper)
        => new(Items.Select(mapper).ToList(), TotalCount, Page, PageSize);

    public override string ToString()
        => $"Page {Page}/{TotalPages}, Items={Items.Count}, TotalCount={TotalCount}, PageSize={PageSize}";
}
