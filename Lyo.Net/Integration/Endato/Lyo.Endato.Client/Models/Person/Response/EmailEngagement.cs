namespace Lyo.Endato.Client.Models.Person.Response;

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
    bool IsBounce);