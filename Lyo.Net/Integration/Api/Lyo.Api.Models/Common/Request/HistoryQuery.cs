using System.Diagnostics;
using Lyo.Query.Models.Common;

namespace Lyo.Api.Models.Common.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class HistoryQuery
{
    public int? Start { get; set; }

    public int? Amount { get; set; }

    public List<SortBy> SortBy { get; set; } = [];

    public WhereClause? WhereClause { get; set; }

    public DateTime? FromDateTime { get; set; }

    public DateTime? ToDateTime { get; set; }

    public override string ToString()
        => $"{FromDateTime:g} - {ToDateTime:g}, {(Start.HasValue ? $"Start={Start}, " : "")}{(Amount.HasValue ? $"Amount={Amount}, " : "")}SortBys={SortBy.Count}";
}