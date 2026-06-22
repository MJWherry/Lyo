using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>Email address linked to a Person Search result.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Email(
    string EmailAddress,
    EmailEngagement? EmailEngagementData,
    int EmailOrdinal,
    bool IsPremium,
    int NonBusiness,
    string? SourceSummary = null)
{
    public override string ToString()
        => $"Email: '{EmailAddress}', Ordinal={EmailOrdinal}, Premium={IsPremium}, NonBusiness={NonBusiness}, Engagement={EmailEngagementData != null}, SourceSummary='{SourceSummary}'";
}
