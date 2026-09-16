using Lyo.Law.Models.Enums;

namespace Lyo.Law.Models.Response;

/// <summary>A child or ancestor heading. Body is omitted.</summary>
public sealed class LawNodeChildRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Outline level.</summary>
    public LawNodeKind Kind { get; set; }

    /// <summary>Order among siblings.</summary>
    public int SortOrder { get; set; }

    /// <summary>Local designator.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Catchline or heading.</summary>
    public string Heading { get; set; } = string.Empty;

    /// <summary>Display citation.</summary>
    public string? Citation { get; set; }

    /// <summary>Importer key.</summary>
    public string SourceKey { get; set; } = string.Empty;
}
