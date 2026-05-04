using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Email(string EmailAddress, EmailEngagement? EmailEngagementData, int EmailOrdinal, bool IsPremium, int NonBusiness)
{
    public override string ToString()
        => $"Email: '{EmailAddress}', Ordinal={EmailOrdinal}, Premium={IsPremium}, NonBusiness={NonBusiness}, Engagement={EmailEngagementData != null}";
}