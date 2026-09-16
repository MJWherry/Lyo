namespace Lyo.Law.Models.Response;

/// <summary>A stored US state jurisdiction.</summary>
public sealed class LawJurisdictionRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>State key, such as <c>US-PA</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;
}
