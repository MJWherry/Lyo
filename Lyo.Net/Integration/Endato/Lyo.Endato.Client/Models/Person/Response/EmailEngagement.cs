using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

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
    string[] Naics,
    bool IsBounce)
{
    public override string ToString()
        => $"EmailEngagement: Score={EngagementScore}, Matched={IsMatched}, Bounce={IsBounce}, GoodDomain={IsGoodDomain}, " +
           $"Naics={Naics.Length}";
}