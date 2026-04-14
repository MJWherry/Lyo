using Lyo.Query.Models.Common;

namespace Lyo.Query.Models.Common.Request;

/// <summary>Shared execution shape for filtered, paged entity loads (<see cref="QueryReq" /> and projection fallback).</summary>
public interface IQueryExecutionRequest
{
    QueryRequestOptions Options { get; }

    int? Start { get; set; }

    int? Amount { get; set; }

    List<object[]> Keys { get; set; }

    WhereClause? WhereClause { get; set; }

    List<string> Include { get; set; }

    List<SortBy> SortBy { get; set; }
}
