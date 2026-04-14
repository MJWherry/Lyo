namespace Lyo.Scheduler.Models;

/// <summary>Options for the scheduler service.</summary>
public sealed class SchedulerOptions
{
    /// <summary>Interval in milliseconds between checks for due schedules. Default: 10000 (10 seconds).</summary>
    public int CheckIntervalMs { get; set; } = 10_000;

    /// <summary>Whether to enable metrics. Default: true.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Whether to run actions in the background (fire-and-forget) or await them. Default: true (background).</summary>
    public bool RunInBackground { get; set; } = true;

    /// <summary>Maximum duration to allow an action to run before timing out. Null = no timeout. Default: 120 minutes.</summary>
    public TimeSpan? ActionTimeout { get; set; } = TimeSpan.FromMinutes(120);
}