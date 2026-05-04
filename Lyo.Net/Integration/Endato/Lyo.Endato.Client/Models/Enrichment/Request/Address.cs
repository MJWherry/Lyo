using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Request;

[DebuggerDisplay("{ToString(),nq}")]
public class Address
{
    public string? AddressLine1 { get; set; }

    public string AddressLine2 { get; set; } = null!;

    public override string ToString()
        => $"Address: Line1='{AddressLine1}', Line2='{AddressLine2}'";
}