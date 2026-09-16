using Lyo.Law.Models.Enums;

namespace Lyo.Law.Models.Request;

/// <summary>One official note supplied with a node upsert.</summary>
public sealed class LawAnnotationReq
{
    /// <summary>What kind of note this is.</summary>
    public LawAnnotationKind Kind { get; set; }

    /// <summary>Order among the node's notes. Lower comes first.</summary>
    public int Ordinal { get; set; }

    /// <summary>Note text.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Citation this note points at, when it is a cross reference and the target may not be imported yet.</summary>
    public string? RelatedCitation { get; set; }

    /// <summary>Imported node this note points at, when the target is already stored.</summary>
    public Guid? RelatedNodeId { get; set; }
}
