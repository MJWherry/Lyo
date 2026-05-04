using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Phone(string FirstReportedDate, string LastReportedDate, string Type, bool IsConnected, string Number)
{
    public override string ToString()
        => $"Phone: '{Number}', Type='{Type}', Connected={IsConnected}, Reported {FirstReportedDate}–{LastReportedDate}";
}