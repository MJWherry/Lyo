namespace Lyo.Trakt.Client;

/// <summary>One page of Trakt results, including the <c>X-Pagination-*</c> headers.</summary>
public sealed class TraktPage<T>
{
    /// <summary>Rows in this page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Page number from <c>X-Pagination-Page</c>.</summary>
    public int Page { get; init; }

    /// <summary>Page size from <c>X-Pagination-Limit</c>.</summary>
    public int Limit { get; init; }

    /// <summary>Page count from <c>X-Pagination-Page-Count</c>.</summary>
    public int PageCount { get; init; }

    /// <summary>Item count from <c>X-Pagination-Item-Count</c>.</summary>
    public int ItemCount { get; init; }
}