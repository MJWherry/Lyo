namespace Lyo.Schedule.Models;

/// <summary>Whether the cron expression includes a leading seconds field.</summary>
public enum CronFormat
{
    /// <summary>Standard 5-field form: minute hour day-of-month month day-of-week.</summary>
    Standard,

    /// <summary>Extended 6-field form: second minute hour day-of-month month day-of-week.</summary>
    IncludeSeconds
}