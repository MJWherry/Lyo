using Lyo.Law.Models.Enums;

namespace Lyo.Law.Postgres.Database;

/// <summary>An official note on a statute node.</summary>
public sealed class LawAnnotationEntity
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning node.</summary>
    public Guid NodeId { get; set; }

    /// <summary>What kind of note this is.</summary>
    public LawAnnotationKind Kind { get; set; }

    /// <summary>Order among the node's notes.</summary>
    public int Ordinal { get; set; }

    /// <summary>Note text.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Citation this note points at.</summary>
    public string? RelatedCitation { get; set; }

    /// <summary>Imported node this note points at.</summary>
    public Guid? RelatedNodeId { get; set; }

    /// <summary>When the row was inserted.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Owning node.</summary>
    public LawNodeEntity Node { get; set; } = null!;
}
