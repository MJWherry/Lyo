namespace Lyo.Law.Models.Request;

/// <summary>Create or update one named statutory compilation inside a state.</summary>
public sealed class LawCodeReq
{
    /// <summary>Owning jurisdiction. Store saves that take a jurisdiction key ignore this until the row exists.</summary>
    public Guid JurisdictionId { get; set; }

    /// <summary>Key unique inside the jurisdiction, such as <c>consolidated-statutes</c> or <c>penal</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Citation prefix, such as <c>Pa.C.S.</c> or <c>Cal. Penal Code</c>.</summary>
    public string? CitationPrefix { get; set; }

    /// <summary>Host the official text is published on, when there is one.</summary>
    public string? SourceHost { get; set; }
}
