using System.Diagnostics;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Common.Request;

/// <summary>Join source extending <see cref="FromClause" /> with join type, ON predicates, and optional result name.</summary>
[DebuggerDisplay("{Type} {Alias}:{EntityType} as {As}")]
public sealed class JoinClause : FromClause
{
    /// <summary>Join kind (v1: <see cref="JoinType.Inner" /> or <see cref="JoinType.Left" />).</summary>
    public JoinType Type { get; set; } = JoinType.Left;

    /// <summary>Equality ON clauses (at least one required).</summary>
    public List<JoinOn> On { get; set; } = [];

    /// <summary>Optional result key for the joined row bag (e.g. <c>recipient</c>). Defaults to <see cref="FromClause.Alias" /> when omitted.</summary>
    public string? As { get; set; }
}
