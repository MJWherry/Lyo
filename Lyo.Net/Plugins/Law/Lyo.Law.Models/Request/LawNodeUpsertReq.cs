using Lyo.Law.Models.Enums;

namespace Lyo.Law.Models.Request;

/// <summary>Create or replace one statute node addressed by jurisdiction, code, and an opaque source key.</summary>
public sealed class LawNodeUpsertReq
{
    /// <summary>State key, such as <c>US-PA</c>.</summary>
    public string JurisdictionKey { get; set; } = string.Empty;

    /// <summary>Code key inside that state.</summary>
    public string CodeKey { get; set; } = string.Empty;

    /// <summary>Importer key unique inside the code. Pennsylvania may use a palegis query string. The value is not parsed.</summary>
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>Source key of the parent node in the same code. Empty means a root under the code. The parent must already exist.</summary>
    public string? ParentSourceKey { get; set; }

    /// <summary>Outline level.</summary>
    public LawNodeKind Kind { get; set; }

    /// <summary>Order among siblings.</summary>
    public int SortOrder { get; set; }

    /// <summary>Local designator, such as <c>67</c>, <c>1105</c>, or <c>(a)</c>.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Catchline or heading.</summary>
    public string Heading { get; set; } = string.Empty;

    /// <summary>Operative text. Empty stores null.</summary>
    public string? Body { get; set; }

    /// <summary>Display citation, such as <c>67 Pa.C.S. § 1105</c>. Empty stores null. Unique per code when set.</summary>
    public string? Citation { get; set; }

    /// <summary>Whether the provision is repealed.</summary>
    public bool IsRepealed { get; set; }

    /// <summary>Page the text came from.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Official notes. Replaced as a set when the content hash changes.</summary>
    public List<LawAnnotationReq> Annotations { get; set; } = [];
}
