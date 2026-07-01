using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>High-risk address marker metadata from Person Search.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record HighRiskMarker(bool IsHighRisk, string? Sic, string? AddressType)
{
    public override string ToString() => $"HighRiskMarker: IsHighRisk={IsHighRisk}, Sic='{Sic}', AddressType='{AddressType}'";
}