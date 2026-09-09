namespace Lyo.DateAndTime;

/// <summary>How a holiday's calendar date is chosen.</summary>
public enum HolidayDateRule
{
    /// <summary>Rule is unknown or not specified.</summary>
    Unknown = 0,

    /// <summary>The holiday falls on a fixed month/day every year.</summary>
    FixedDate,

    /// <summary>The holiday falls on the nth weekday of a month.</summary>
    NthWeekdayOfMonth,

    /// <summary>The holiday falls on the last weekday of a month.</summary>
    LastWeekdayOfMonth
}