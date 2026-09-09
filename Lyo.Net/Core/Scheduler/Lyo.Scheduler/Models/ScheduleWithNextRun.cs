namespace Lyo.Scheduler.Models;

/// <summary>A schedule paired with its computed next run time.</summary>
/// <param name="Schedule">Schedule metadata.</param>
/// <param name="NextRun">Next run (UTC), or null when nothing is upcoming.</param>
public sealed record ScheduleWithNextRun(ScheduleInfo Schedule, DateTime? NextRun);