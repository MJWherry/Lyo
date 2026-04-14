namespace Lyo.Schedule.Models;

/// <summary>Type of schedule determining when the action executes.</summary>
public enum ScheduleType
{
    /// <summary>Execute at specific times each scheduled day.</summary>
    SetTimes,

    /// <summary>Execute at intervals within a time window each scheduled day.</summary>
    Interval,

    /// <summary>Execute once at a specific date and time.</summary>
    OneShot
}