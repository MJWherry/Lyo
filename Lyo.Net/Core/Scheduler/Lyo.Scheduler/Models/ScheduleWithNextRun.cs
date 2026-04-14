namespace Lyo.Scheduler.Models;

/// <summary>A schedule with its computed next run time.</summary>
/// <param name="Schedule">The schedule info.</param>
/// <param name="NextRun">The next run time (UTC), or null if the schedule has no upcoming run.</param>
public sealed record ScheduleWithNextRun(ScheduleInfo Schedule, DateTime? NextRun);