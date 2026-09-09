namespace Lyo.Schedule.Models;

/// <summary>Kind of schedule that decides when the action fires.</summary>
public enum ScheduleType
{
    /// <summary>Fire at specific clock times on each scheduled day.</summary>
    SetTimes,

    /// <summary>Fire at intervals inside a time window on each scheduled day.</summary>
    Interval,

    /// <summary>Fire once at a specific date and time.</summary>
    OneShot,

    /// <summary>Fire according to a standard 5- or 6-field cron expression (for example <c>"0 8 * * MON-FRI"</c>).</summary>
    Cron
}