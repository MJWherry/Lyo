using System.Diagnostics;
using Lyo.Query.Models.Common;

namespace Lyo.Api.Models.Common.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class PatchRequest
{
    public List<object[]>? Keys { get; set; }

    /// <summary>WhereClause used to find entities to patch (e.g. ConditionClause or GroupClause And of conditions).</summary>
    public WhereClause? Query { get; set; }

    public Dictionary<string, object?> Properties { get; set; } = new();

    /// <summary>If using bulk endpoints, specifies that request can modify multiple objects based on identifiers</summary>
    public bool AllowMultiple { get; set; }

    public PatchRequest() { }

    public PatchRequest(IEnumerable<object[]> keys) => Keys = keys.ToList();

    public PatchRequest(WhereClause query) => Query = query;

    public override string ToString() => $"Properties={Properties.Count} Keys={Keys?.Count} Query={Query != null}";
}