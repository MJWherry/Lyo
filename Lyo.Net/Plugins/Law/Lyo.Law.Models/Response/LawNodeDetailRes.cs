using Lyo.Law.Models.Enums;

namespace Lyo.Law.Models.Response;

/// <summary>A statute node with its operative text and official notes.</summary>
public sealed class LawNodeDetailRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning code.</summary>
    public Guid CodeId { get; set; }

    /// <summary>State key of the owning code.</summary>
    public string JurisdictionKey { get; set; } = string.Empty;

    /// <summary>Key of the owning code.</summary>
    public string CodeKey { get; set; } = string.Empty;

    /// <summary>Parent node. Null for a root under the code.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Parent source key in the same code. Null when this node is a root.</summary>
    public string? ParentSourceKey { get; set; }

    /// <summary>Importer key unique inside the code.</summary>
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>Page the text came from.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Outline level.</summary>
    public LawNodeKind Kind { get; set; }

    /// <summary>Order among siblings.</summary>
    public int SortOrder { get; set; }

    /// <summary>Local designator.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Catchline or heading.</summary>
    public string Heading { get; set; } = string.Empty;

    /// <summary>Operative text.</summary>
    public string? Body { get; set; }

    /// <summary>Display citation.</summary>
    public string? Citation { get; set; }

    /// <summary>Whether the provision is repealed.</summary>
    public bool IsRepealed { get; set; }

    /// <summary>SHA-256 hex of the stored content.</summary>
    public string ContentSha256 { get; set; } = string.Empty;

    /// <summary>Official notes, ordinal order.</summary>
    public List<LawAnnotationRes> Annotations { get; set; } = [];
}