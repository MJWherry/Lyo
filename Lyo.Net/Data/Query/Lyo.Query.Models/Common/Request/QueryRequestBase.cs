using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lyo.Query.Models.Common.Request;

/// <summary>Common fields for <see cref="QueryConcreteReq" />, <see cref="ProjectionQueryReq" />, and root <see cref="QueryReq" />.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(QueryConcreteReq), "concrete")]
[JsonDerivedType(typeof(ProjectionQueryReq), "project")]
[JsonDerivedType(typeof(QueryReq), "root")]
[DebuggerDisplay("{ToString(),nq}")]
public abstract class QueryRequestBase
{
    /// <summary>Zero-based offset for paging (skip).</summary>
    public int? Start { get; set; }

    /// <summary>Maximum number of rows to return (take).</summary>
    public int? Amount { get; set; }

    /// <summary>
    /// Optional list of primary key values to fetch specific entities. Each element is a key array. Single-key entities: use one value per row, e.g. [[1], [2], [3]] for ids 1,
    /// 2, 3. Composite-key entities: use multiple values per row in key order, e.g. [["tenant-a", 1], ["tenant-b", 2]] for (TenantId, Id).
    /// </summary>
    public List<object[]> Keys { get; set; } = [];

    /// <summary>Optional filter tree applied to the query.</summary>
    public WhereClause? WhereClause { get; set; }

    /// <summary>Must match database entity property or decorate the response entity property with DatabaseNameAttribute</summary>
    public List<string> Include { get; set; } = [];

    /// <summary>Ordered collection of sort keys (see <see cref="SortBy.Priority" /> for cross-property ordering).</summary>
    public List<SortBy> SortBy { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"Start={Start}, Amount={Amount}, Keys={Keys.Count}, Includes={Include.Count}, SortBys={SortBy.Count}";
}