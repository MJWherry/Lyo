using System.Diagnostics;

namespace Lyo.Scheduler.Models;

/// <summary>Configuration for the scheduler service.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SchedulerOptions
{
    /// <summary>
    /// Milliseconds between due-schedule checks. Default: 10000 (10 seconds).
    /// <para>
    /// <b>Look-ahead window:</b> A schedule is due when its next fire time falls within <c>now + CheckIntervalMs + 1000 ms</c>. That window avoids missed
    /// fires when the check runs a bit late versus the scheduled time. For tight timing (for example the schedule interval ≈ the check interval), reduce
    /// <c>CheckIntervalMs</c> so the look-ahead is a smaller share of the period.
    /// </para>
    /// </summary>
    public int CheckIntervalMs { get; set; } = 10_000;

    /// <summary>True when metrics are recorded. Default: true.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>True to run actions in the background (fire-and-forget); false to await them. Default: true (background).</summary>
    public bool RunInBackground { get; set; } = true;

    /// <summary>Longest time an action may run before it times out. Null means no timeout. Default: 120 minutes.</summary>
    public TimeSpan? ActionTimeout { get; set; } = TimeSpan.FromMinutes(120);

    /// <summary>
    /// How many days ahead to search when computing the next scheduled run. Default: 366 days so monthly schedules work (for example a job on the 1st of a
    /// specific month may need up to ~365 days of look-ahead).
    /// </summary>
    public int MaxDaysLookAhead { get; set; } = 366;

    public override string ToString()
        => $"SchedulerOptions: checkIntervalMs={CheckIntervalMs}, metrics={EnableMetrics}, background={RunInBackground}, timeout={ActionTimeout}, lookAheadDays={MaxDaysLookAhead}";
}