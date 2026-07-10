using System.Diagnostics;

namespace Lyo.Query.Models.Common.Request;

/// <summary>
/// Request body for root <c>/Query</c> (context base, not under <c>{entityType}</c>) — From/Joins + sparse Select.
/// Returns the same projected row shape as <c>/QueryProject</c>.
/// </summary>
/// <remarks>
/// <see cref="QueryRequestBase.Include" /> is forbidden. Outer <see cref="QueryRequestBase.WhereClause" /> / <see cref="QueryRequestBase.SortBy" />
/// may only reference the From alias in v1 (paging safety). Joins are arbitrary EF Join/GroupJoin on ON columns
/// (including chained join aliases) — not navigations (<c>/QueryProject</c> owns nav Select).
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class QueryReq : QueryRequestBase, IQueryExecutionRequest
{
    public ProjectedQueryRequestOptions Options { get; set; } = new();

    /// <summary>Required root source (entity type + alias).</summary>
    public FromClause From { get; set; } = new();

    /// <summary>Optional joins onto <see cref="From" /> (v1: many-to-one / one-to-one relative to From).</summary>
    public List<JoinClause> Joins { get; set; } = [];

    /// <summary>Projected field paths (alias.column). At least one required.</summary>
    public List<string> Select { get; set; } = [];

    /// <summary>
    /// Optional computed fields (SmartFormat). Placeholders use Select paths as <c>{alias.property}</c>
    /// (Mustache <c>{{alias.property}}</c> is also accepted). From-only templates become a root scalar;
    /// any join placeholder is written only onto each bag of the deepest join alias referenced
    /// (From values are repeated per bag when formatting).
    /// </summary>
    public List<ComputedField> ComputedFields { get; set; } = [];

    QueryRequestOptions IQueryExecutionRequest.Options => Options;

    public override string ToString()
        => $"From={From.Alias}:{From.EntityType}, Joins={Joins.Count}, Selects={Select.Count}, ComputedFields={ComputedFields.Count}, SortBys={SortBy.Count}, {Options}";
}
