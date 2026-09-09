using System.Diagnostics;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Common.Request;

/// <summary>Join source that extends <see cref="FromClause" /> with join type, ON predicates, and an optional result name.</summary>
[DebuggerDisplay("{Type} {Alias}:{EntityType} as {As}")]
public sealed class JoinClause : FromClause
{
    /// <summary>Join kind: <see cref="JoinType.Inner" />, <see cref="JoinType.Left" />, <see cref="JoinType.Right" />, or <see cref="JoinType.FullOuter" />.</summary>
    public JoinType Type { get; set; } = JoinType.Left;

    /// <summary>Equality ON clauses (at least one needed).</summary>
    public List<JoinOn> On { get; set; } = [];

    /// <summary>Optional result key for the joined row bag (e.g. <c>recipient</c>). Starts as <see cref="FromClause.Alias" /> when omitted.</summary>
    public string? As { get; set; }
}