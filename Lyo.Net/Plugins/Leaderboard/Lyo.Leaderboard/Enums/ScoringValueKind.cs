namespace Lyo.Leaderboard.Enums;

/// <summary>Stored type of a scoring property value.</summary>
public enum ScoringValueKind
{
    /// <summary>Dropdown / discrete key.</summary>
    Choice = 0,

    /// <summary>Decimal number.</summary>
    Number = 1,

    /// <summary>Whole number.</summary>
    Integer = 2,

    /// <summary>True / false.</summary>
    Boolean = 3,

    /// <summary>Free text (usually not scored).</summary>
    Text = 4,

    /// <summary>Duration stored as seconds in <c>number_value</c>.</summary>
    Duration = 5,

    /// <summary>Point in time.</summary>
    DateTime = 6
}