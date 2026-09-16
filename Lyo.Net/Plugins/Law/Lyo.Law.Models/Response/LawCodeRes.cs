namespace Lyo.Law.Models.Response;

/// <summary>A stored statutory compilation.</summary>
public sealed class LawCodeRes
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
}
