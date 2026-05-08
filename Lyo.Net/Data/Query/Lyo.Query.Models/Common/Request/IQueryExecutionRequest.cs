namespace Lyo.Query.Models.Common.Request;

/// <summary>Shared execution shape for filtered, paged entity loads (<see cref="QueryReq" /> and projection fallback).</summary>
public interface IQueryExecutionRequest
{
    /// <summary>Options that control total count, include filtering, and projection-specific behavior (concrete type depends on <see cref="QueryReq" /> vs <see cref="ProjectionQueryReq" />).</summary>
    QueryRequestOptions Options { get; }

    /// <summary>Zero-based offset for paging.</summary>
    int? Start { get; set; }

    /// <summary>Maximum number of rows to return.</summary>
    int? Amount { get; set; }

    /// <summary>Explicit primary-key rows to fetch; each row is an array of key values in composite-key order.</summary>
    List<object[]> Keys { get; set; }

    /// <summary>Optional filter tree.</summary>
    WhereClause? WhereClause { get; set; }

    /// <summary>Navigation paths to eager-load (full query); projection uses <see cref="ProjectionQueryReq.Select" /> instead for shape.</summary>
    List<string> Include { get; set; }

    /// <summary>Sort specifications applied after filtering.</summary>
    List<SortBy> SortBy { get; set; }
}