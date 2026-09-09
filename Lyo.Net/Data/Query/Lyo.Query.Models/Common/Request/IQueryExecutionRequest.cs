namespace Lyo.Query.Models.Common.Request;

/// <summary>Shared execution contract for filtered, paged loads (<see cref="QueryConcreteReq" />, <see cref="ProjectionQueryReq" />, <see cref="QueryReq" />).</summary>
public interface IQueryExecutionRequest
{
    /// <summary>Options for total count, include filtering, and projection-specific behavior (concrete type depends on the request DTO).</summary>
    QueryRequestOptions Options { get; }

    /// <summary>Zero-based paging offset.</summary>
    int? Start { get; set; }

    /// <summary>Max rows to return.</summary>
    int? Amount { get; set; }

    /// <summary>Explicit primary-key rows to fetch; each row is a key-value array in composite-key order.</summary>
    List<object[]> Keys { get; set; }

    /// <summary>Optional filter tree for this request.</summary>
    WhereClause? WhereClause { get; set; }

    /// <summary>Navigation paths to eager-load (full query); projection uses <see cref="ProjectionQueryReq.Select" /> for shape instead.</summary>
    List<string> Include { get; set; }

    /// <summary>Sort specs applied after filtering.</summary>
    List<SortBy> SortBy { get; set; }
}