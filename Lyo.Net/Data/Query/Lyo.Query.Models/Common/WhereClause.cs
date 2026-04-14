using System.Text.Json.Serialization;

namespace Lyo.Query.Models.Common;

[JsonDerivedType(typeof(ConditionClause), "condition")]
[JsonDerivedType(typeof(GroupClause), "group")]
public abstract class WhereClause
{
    public string? Description { get; set; }

    /// <summary>Optional nested where-clause applied after this clause (e.g. two-phase: root in DB, nested filter in-memory).</summary>
    public WhereClause? SubClause { get; set; }

    /// <summary>Pretty-print the clause with the given indentation depth (each depth = 2 spaces). Default implementation falls back to ToString(); derived classes may override.</summary>
    public abstract string Print(int indent = 0);

    public override string ToString() => Description ?? GetType().Name;
}
