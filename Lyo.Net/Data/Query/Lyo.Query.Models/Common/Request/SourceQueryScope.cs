using System.Diagnostics;

namespace Lyo.Query.Models.Common.Request;

/// <summary>
/// Optional filter scope applied to a From/Join source <c>DbSet</c> before the join. Not the same as <see cref="WhereClause.SubClause" /> (two-phase in-memory). v1 allows
/// Where + Keys only.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SourceQueryScope
{
    public WhereClause? WhereClause { get; set; }

    public List<object[]> Keys { get; set; } = [];

    public override string ToString() => $"Where={(WhereClause != null ? "yes" : "no")}, Keys={Keys.Count}";
}