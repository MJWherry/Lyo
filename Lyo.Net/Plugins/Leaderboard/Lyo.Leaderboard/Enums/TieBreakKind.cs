namespace Lyo.Leaderboard.Enums;

/// <summary>How equal totals are ordered.</summary>
public enum TieBreakKind
{
    /// <summary>Earlier first approved submission wins.</summary>
    FirstSubmitted = 0,

    /// <summary>Later approved submission wins.</summary>
    LastSubmitted = 1,

    /// <summary>Compare a named property's latest approved raw number.</summary>
    NamedProperty = 2
}
