using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Request;

/// <summary>Address fields used in Contact Enrichment requests.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class Address
{
    /// <summary>First address line.</summary>
    public string? AddressLine1 { get; set; }

    /// <summary>Second address line.</summary>
    public string? AddressLine2 { get; set; }

    public override string ToString() => $"Address: Line1='{AddressLine1}', Line2='{AddressLine2}'";
}