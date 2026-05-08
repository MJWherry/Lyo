using System.Text.Json.Serialization;

namespace Lyo.Query.Models.Common;

/// <summary>Abstract syntax tree node for filter clauses: either a field predicate (<see cref="ConditionClause" />) or a boolean group (<see cref="GroupClause" />).</summary>
[JsonDerivedType(typeof(ConditionClause), "condition")]
[JsonDerivedType(typeof(GroupClause), "group")]
public abstract class WhereClause
{
    /// <summary>Optional human-readable label for diagnostics, logging, or UI (not evaluated as SQL).</summary>
    public string? Description { get; set; }

    /// <summary>Optional nested where-clause applied after this clause (e.g. two-phase: root in DB, nested filter in-memory).</summary>
    public WhereClause? SubClause { get; set; }

    /// <summary>Pretty-print the clause with the given indentation depth (each depth = 2 spaces). Default implementation falls back to ToString(); derived classes may override.</summary>
    public abstract string Print(int indent = 0);

    public override string ToString() => Description ?? GetType().Name;
}