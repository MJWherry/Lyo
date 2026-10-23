using System.Diagnostics;

namespace Lyo.Diagram.Postgres.Database;

/// <summary>One persisted diagram. The page and cell model lives in <see cref="DocumentJson" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DiagramEntity
{
    /// <summary>Primary key. Matches <see cref="Lyo.Diagram.Models.DiagramDocument.Id" />.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name, duplicated from the document so a list does not read the JSON payload.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Pages and cells as JSON (<c>jsonb</c>).</summary>
    public string DocumentJson { get; set; } = string.Empty;

    /// <summary>When the row was inserted, UTC.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When the row was last updated, UTC. Null until the first update.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Type name, id, and name.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Name}";
}