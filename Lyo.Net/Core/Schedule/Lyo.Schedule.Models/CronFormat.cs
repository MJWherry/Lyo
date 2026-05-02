namespace Lyo.Schedule.Models;

/// <summary>Indicates whether the cron expression includes a leading seconds field.</summary>
public enum CronFormat
{
    /// <summary>Standard 5-field format: minute hour day-of-month month day-of-week.</summary>
    Standard,

    /// <summary>Extended 6-field format: second minute hour day-of-month month day-of-week.</summary>
    IncludeSeconds
}