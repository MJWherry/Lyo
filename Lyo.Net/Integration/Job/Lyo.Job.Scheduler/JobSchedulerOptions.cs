using Lyo.Common.Enums;

namespace Lyo.Job.Scheduler;

/// <summary>Options for the Job Scheduler service.</summary>
public sealed class JobSchedulerOptions
{
    /// <summary>Base URL of the Job API (e.g. https://api.example.com). Used for querying definitions and creating runs.</summary>
    public required string ApiBaseUrl { get; set; }

    /// <summary>US state used for timezone when evaluating schedule times. Defaults to PA.</summary>
    public USState TimezoneState { get; set; } = USState.PA;

    /// <summary>Interval in seconds between definition refresh. Default 30.</summary>
    public int DefinitionRefreshIntervalSeconds { get; set; } = 30;

    /// <summary>Interval in seconds between schedule checks. Default 10.</summary>
    public int ScheduleCheckIntervalSeconds { get; set; } = 10;

    /// <summary>Identity used as CreatedBy when the scheduler creates job runs.</summary>
    public string CreatedBy { get; set; } = "Scheduler";
}