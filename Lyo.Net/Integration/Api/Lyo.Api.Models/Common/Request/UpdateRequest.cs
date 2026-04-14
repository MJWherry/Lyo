using System.Diagnostics;
using Lyo.Query.Models.Common;

namespace Lyo.Api.Models.Common.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class UpdateRequest<T>
{
    public object[]? Keys { get; set; }

    /// <summary>WhereClause used to find entities to update (e.g. ConditionClause or GroupClause And of conditions).</summary>
    public WhereClause? Query { get; set; }

    public T Data { get; set; } = default!;

    public UpdateRequest() { }

    public UpdateRequest(IEnumerable<object> keys, T data)
    {
        Keys = keys.ToArray();
        Data = data;
    }

    public UpdateRequest(T data, params object[] keys)
    {
        Data = data;
        Keys = keys;
    }

    public override string ToString() => $"{typeof(T).Name} Keys={Keys?.Length} Query={Query != null}";
}