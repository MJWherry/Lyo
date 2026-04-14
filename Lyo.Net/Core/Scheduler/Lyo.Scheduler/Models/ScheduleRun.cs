namespace Lyo.Scheduler.Models;

/// <summary>A scheduled run: a specific schedule executing at a specific time.</summary>
/// <param name="Schedule">The schedule info.</param>
/// <param name="RunAt">When the schedule will run (UTC).</param>
public sealed record ScheduleRun(ScheduleInfo Schedule, DateTime RunAt);