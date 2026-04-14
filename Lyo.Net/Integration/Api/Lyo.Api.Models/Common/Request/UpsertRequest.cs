using System.Diagnostics;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Models.Common.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class UpsertRequest<T>
{
    public object[]? Keys { get; set; }

    /// <summary>WhereClause used to find entities to upsert (e.g. ConditionClause or GroupClause And of conditions).</summary>
    public WhereClause? Query { get; set; }

    public T NewData { get; set; } = default!;

    /// <summary>Will ignore properties that determines if the object should be updated IE metadata properties like ModifiedOn/ModifiedBy</summary>
    public List<string> IgnoredCompareProperties { get; set; } = [];

    public UpsertRequest() { }

    public UpsertRequest(T request, WhereClause? query = null)
    {
        Query = query;
        NewData = request;
    }

    public UpsertRequest(T request, string propertyName, object? value = null, ComparisonOperatorEnum comparator = ComparisonOperatorEnum.Equals)
    {
        Query = new ConditionClause(propertyName, comparator, value);
        NewData = request;
    }

    public override string ToString() => $"Query={Query != null}";
}