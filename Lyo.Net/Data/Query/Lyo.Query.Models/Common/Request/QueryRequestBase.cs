using System.Diagnostics;
using Lyo.Query.Models.Common;

namespace Lyo.Query.Models.Common.Request;

/// <summary>Common fields for <see cref="QueryReq" /> and <see cref="ProjectionQueryReq" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class QueryRequestBase
{
    public int? Start { get; set; }

    public int? Amount { get; set; }

    /// <summary>
    /// Optional list of primary key values to fetch specific entities. Each element is a key array. Single-key entities: use one value per row, e.g. [[1], [2], [3]] for ids 1,
    /// 2, 3. Composite-key entities: use multiple values per row in key order, e.g. [["tenant-a", 1], ["tenant-b", 2]] for (TenantId, Id).
    /// </summary>
    public List<object[]> Keys { get; set; } = [];

    public WhereClause? WhereClause { get; set; }

    /// <summary>Must match database entity property or decorate the response entity property with DatabaseNameAttribute</summary>
    public List<string> Include { get; set; } = [];

    public List<SortBy> SortBy { get; set; } = [];
}
