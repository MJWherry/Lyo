namespace Lyo.Scheduler.Models;

/// <summary>Options for the scheduler service.</summary>
public sealed class SchedulerOptions
{
    /// <summary>
    /// Interval in milliseconds between checks for due schedules. Default: 10000 (10 seconds).
    /// <para>
    /// <b>Look-ahead window:</b> A schedule is considered due when its next fire time falls within <c>now + CheckIntervalMs + 1000 ms</c>. This intentional window prevents missed
    /// fires when the check runs slightly late relative to the scheduled time. For tight timing requirements (e.g. the schedule interval ≈ the check interval), reduce
    /// <c>CheckIntervalMs</c> so the look-ahead is a smaller fraction of the period.
    /// </para>
    /// </summary>
    public int CheckIntervalMs { get; set; } = 10_000;

    /// <summary>Whether to enable metrics. Default: true.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Whether to run actions in the background (fire-and-forget) or await them. Default: true (background).</summary>
    public bool RunInBackground { get; set; } = true;

    /// <summary>Maximum duration to allow an action to run before timing out. Null = no timeout. Default: 120 minutes.</summary>
    public TimeSpan? ActionTimeout { get; set; } = TimeSpan.FromMinutes(120);

    /// <summary>
    /// Maximum number of days to look ahead when calculating the next scheduled run. Default: 366 days to support monthly schedules (e.g. a job scheduled on the 1st of a
    /// specific month requires looking up to ~365 days ahead).
    /// </summary>
    public int MaxDaysLookAhead { get; set; } = 366;
}