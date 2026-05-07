using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Address(string FirstReportedDate, string LastReportedDate, string Street, string? Unit, string City, string State, string Zip)
{
    public override string ToString() => $"Address: '{Street}' Unit='{Unit}', '{City}', {State} {Zip}, Reported {FirstReportedDate}–{LastReportedDate}";
}