using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Request;

/// <summary>Address parts used in Person Search address criteria.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PersonQueryAddress
{
    /// <summary>Primary address line.</summary>
    public string? AddressLine1 { get; set; }

    /// <summary>Secondary address line (city/state/zip when supplied as one field).</summary>
    public string? AddressLine2 { get; set; }

    /// <summary>County filter.</summary>
    public string? County { get; set; }

    public override string ToString() => $"PersonQueryAddress: Line1='{AddressLine1}', Line2='{AddressLine2}', County='{County}'";
}
