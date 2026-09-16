namespace Lyo.Leaderboard.Enums;

/// <summary>How approved entry scores collapse into a member standing.</summary>
public enum AggregationKind
{
    /// <summary>Sum of approved entry scores.</summary>
    Sum = 0,

    /// <summary>Single best entry (max when high wins, min when low wins).</summary>
    Best = 1,

    /// <summary>Sum of the best <c>N</c> entries.</summary>
    BestN = 2,

    /// <summary>Mean of approved entry scores.</summary>
    Average = 3,

    /// <summary>Count of approved entries (score ignored).</summary>
    Count = 4,

    /// <summary>Score of the most recently occurred approved entry.</summary>
    Latest = 5
}
