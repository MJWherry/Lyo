using System.Diagnostics;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Common.Request;

[DebuggerDisplay("{ToString(),nq}")]
public class QueryRequestOptions
{
    public QueryTotalCountMode TotalCountMode { get; set; } = QueryTotalCountMode.Exact;

    public QueryIncludeFilterMode IncludeFilterMode { get; set; } = QueryIncludeFilterMode.Full;

    public override string ToString()
        => $"TotalCountMode: {TotalCountMode}, IncludeFilterMode: {IncludeFilterMode}";
}