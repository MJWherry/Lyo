using System.Diagnostics;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Common.Request;

[DebuggerDisplay("{ToString(),nq}")]
public class QueryRequestOptions
{
    /// <summary>Whether and how to compute total row counts for paging UI.</summary>
    public QueryTotalCountMode TotalCountMode { get; set; } = QueryTotalCountMode.Exact;

    /// <summary>Whether included navigation graphs are filtered to rows matching the filter or fully expanded.</summary>
    public QueryIncludeFilterMode IncludeFilterMode { get; set; } = QueryIncludeFilterMode.Full;

    public override string ToString() => $"TotalCountMode: {TotalCountMode}, IncludeFilterMode: {IncludeFilterMode}";
}