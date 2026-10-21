using System.Diagnostics;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Common.Request;

/// <summary>
/// One aggregate column on a <see cref="ProjectionQueryReq" />. When <see cref="ProjectionQueryReq.Aggregates" /> is non-empty, <see cref="ProjectionQueryReq.Select" /> is
/// the group key and each aggregate is an extra column on the group row. An empty <see cref="ProjectionQueryReq.Select" /> is one summary row over the filtered set.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AggregateSelect
{
    /// <summary>Which aggregate to compute.</summary>
    public AggregateFunction Function { get; set; }

    /// <summary>Output column name on each result row.</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Source path. Optional for <see cref="AggregateFunction.Count" />: omit it to count rows in the group, pass a collection (or a <c>.count</c> path) to total its elements,
    /// or pass a scalar to count non-null values. Required for every other function.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>Builds an empty aggregate column.</summary>
    public AggregateSelect() { }

    /// <summary>Builds an aggregate column. <paramref name="field" /> is optional for <see cref="AggregateFunction.Count" /> and required for every other function.</summary>
    public AggregateSelect(AggregateFunction function, string name, string? field = null)
    {
        Function = function;
        Name = name;
        Field = field;
    }

    public override string ToString() => Field is { Length: > 0 } ? $"{Name} = {Function}({Field})" : $"{Name} = {Function}";
}