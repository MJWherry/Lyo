using System.Diagnostics;

namespace Lyo.Law.Postgres.Database;

/// <summary>One named statutory compilation inside a state.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class LawCodeEntity
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning jurisdiction.</summary>
    public Guid JurisdictionId { get; set; }

    /// <summary>Key unique inside the jurisdiction.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Citation prefix.</summary>
    public string? CitationPrefix { get; set; }

    /// <summary>Official source host.</summary>
    public string? SourceHost { get; set; }

    /// <summary>When the row was inserted.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When the row was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Owning state.</summary>
    public LawJurisdictionEntity Jurisdiction { get; set; } = null!;

    /// <summary>Nodes in this compilation.</summary>
    public List<LawNodeEntity> Nodes { get; set; } = [];

    /// <summary>Type name, id, and name.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Name}";
}