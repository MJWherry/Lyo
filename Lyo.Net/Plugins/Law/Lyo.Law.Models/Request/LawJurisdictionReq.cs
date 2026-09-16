namespace Lyo.Law.Models.Request;

/// <summary>Create or update a US state jurisdiction. <c>US</c> is rejected.</summary>
public sealed class LawJurisdictionReq
{
    /// <summary>State key, such as <c>US-PA</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name, such as Pennsylvania.</summary>
    public string Name { get; set; } = string.Empty;
}
