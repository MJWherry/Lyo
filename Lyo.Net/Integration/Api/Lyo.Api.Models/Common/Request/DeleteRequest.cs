using System.Diagnostics;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Models.Common.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class DeleteRequest
{
    public List<object[]>? Keys { get; set; }

    /// <summary>WhereClause used to find entities to delete (e.g. ConditionClause or GroupClause And of conditions).</summary>
    public WhereClause? Query { get; set; }

    /// <summary>If using bulk endpoints, specifies that request can modify multiple objects based on identifiers</summary>
    public bool AllowMultiple { get; set; }

    public DeleteRequest() { }

    public DeleteRequest(WhereClause? query = null, bool allowMultiple = false)
    {
        Query = query;
        AllowMultiple = allowMultiple;
    }

    public DeleteRequest(string propertyName, object? value = null, ComparisonOperatorEnum comparator = ComparisonOperatorEnum.Equals, bool allowMultiple = false)
    {
        Query = new ConditionClause(propertyName, comparator, value);
        AllowMultiple = allowMultiple;
    }

    public override string ToString() => $"Query={Query != null} KeyCount={Keys?.Count} AllowMultiple={AllowMultiple}";
}