using System.Diagnostics;

namespace Lyo.Query.Models.Common.Request;

/// <summary>Request body for <c>/Query</c> — full entity graphs (includes, no sparse projection).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class QueryReq : QueryRequestBase, IQueryExecutionRequest
{
    public QueryRequestOptions Options { get; set; } = new();

    QueryRequestOptions IQueryExecutionRequest.Options => Options;

    public override string ToString()
        => $"{(Start.HasValue ? $"Start={Start}, " : "")}{(Amount.HasValue ? $"Amount={Amount}, " : "")}Includes={Include.Count}, SortBys={SortBy.Count}, {Options}";
}
