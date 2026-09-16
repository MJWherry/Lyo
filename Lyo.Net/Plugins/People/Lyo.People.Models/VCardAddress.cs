namespace Lyo.People.Models;

/// <summary>One ADR value. Components with no meaning for the caller are left as text so the caller can drop them.</summary>
public sealed class VCardAddress
{
    /// <summary>Street line.</summary>
    public string? Street { get; set; }

    /// <summary>Locality, usually the city.</summary>
    public string? Locality { get; set; }

    /// <summary>Region, state, or province.</summary>
    public string? Region { get; set; }

    /// <summary>Postal code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Country text.</summary>
    public string? Country { get; set; }
}
