using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>Email engagement and deliverability signals for a person-search email.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailEngagement(
    DateTime LastCheckedDate,
    bool IsGoodDomain,
    bool IsMatched,
    int EngagementScore,
    DateTime LastTouchedDate,
    int SendTime,
    string BestDayOfTheWeek,
    string BestTimeOfTheDay,
    string Frequency,
    IReadOnlyList<string> Naics,
    bool IsBounce)
{
    public override string ToString()
        => $"EmailEngagement: Score={EngagementScore}, Matched={IsMatched}, Bounce={IsBounce}, GoodDomain={IsGoodDomain}, Naics={Naics.Count}";
}
