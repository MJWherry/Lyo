namespace Lyo.DateAndTime;

/// <summary>Represents how a holiday's calendar date is determined.</summary>
public enum HolidayDateRule
{
    /// <summary>Unknown or unspecified rule.</summary>
    Unknown = 0,

    /// <summary>The holiday occurs on a fixed month/day each year.</summary>
    FixedDate,

    /// <summary>The holiday occurs on the nth weekday within a month.</summary>
    NthWeekdayOfMonth,

    /// <summary>The holiday occurs on the last weekday within a month.</summary>
    LastWeekdayOfMonth
}