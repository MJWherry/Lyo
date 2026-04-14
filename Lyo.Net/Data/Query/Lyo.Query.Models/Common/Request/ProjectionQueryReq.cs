using System.Diagnostics;

namespace Lyo.Query.Models.Common.Request;

/// <summary>Request body for <c>/QueryProject</c> — sparse projection (<see cref="Select" />) and optional computed columns.</summary>
/// <remarks>
/// <see cref="QueryRequestBase.Include" /> is not used for QueryProject: EF navigation loads are derived from <see cref="Select" />
/// (and from collection paths referenced in <see cref="QueryRequestBase.WhereClause" />). Client-supplied includes are ignored.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ProjectionQueryReq : QueryRequestBase, IQueryExecutionRequest
{
    public ProjectedQueryRequestOptions Options { get; set; } = new();

    QueryRequestOptions IQueryExecutionRequest.Options => Options;

    /// <summary>Projected field paths. QueryProject requires at least one.</summary>
    public List<string> Select { get; set; } = [];

    /// <summary>
    /// Optional computed fields (SmartFormat). Requires IFormatterService. Placeholders reference <see cref="Select" /> field names.
    /// </summary>
    public List<ComputedField> ComputedFields { get; set; } = [];

    public override string ToString()
        => $"{(Start.HasValue ? $"Start={Start}, " : "")}{(Amount.HasValue ? $"Amount={Amount}, " : "")}Includes={Include.Count}, Selects={Select.Count}, ComputedFields={ComputedFields.Count}, SortBys={SortBy.Count}, {Options}";
}
