namespace Lyo.Scheduler.Models;

/// <summary>One scheduled occurrence: a given schedule firing at a given time.</summary>
/// <param name="Schedule">Schedule metadata.</param>
/// <param name="RunAt">When the schedule will run (UTC).</param>
public sealed record ScheduleRun(ScheduleInfo Schedule, DateTime RunAt);