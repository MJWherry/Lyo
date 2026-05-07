using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Request;

[DebuggerDisplay("{ToString(),nq}")]
public class EnrichmentQuery
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public Address Address { get; set; } = null!;

    public string? DoB { get; set; }

    public override string ToString() => $"EnrichmentQuery: FirstName='{FirstName}', LastName='{LastName}', DoB='{DoB}', Address={Address}";
}