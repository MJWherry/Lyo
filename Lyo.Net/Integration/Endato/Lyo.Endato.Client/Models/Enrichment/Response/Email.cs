using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Enrichment.Response;

/// <summary>Email address returned by Contact Enrichment.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Email(
    string EmailAddress,
    string FirstReportedDate,
    string LastReportedDate,
    string? SourceSummary = null)
{
    public override string ToString()
        => $"Email: '{EmailAddress}', Reported {FirstReportedDate}–{LastReportedDate}, SourceSummary='{SourceSummary}'";
}
