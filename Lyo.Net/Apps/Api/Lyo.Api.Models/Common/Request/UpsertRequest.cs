using System.Diagnostics;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Models.Common.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class UpsertRequest<T>
{
    public object[]? Keys { get; set; }

    /// <summary>WhereClause used to find entities to upsert (for example ConditionClause or a GroupClause And of conditions).</summary>
    public WhereClause? Query { get; set; }

    public T NewData { get; set; } = default!;

    /// <summary>Ignores properties that decide whether the object should be updated, such as metadata like ModifiedOn/ModifiedBy.</summary>
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