using System.Diagnostics;

namespace Lyo.Scheduler.Models;

/// <summary>A schedule paired with its computed next run time.</summary>
/// <param name="Schedule">Schedule metadata.</param>
/// <param name="NextRun">Next run (UTC), or null when nothing is upcoming.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ScheduleWithNextRun(ScheduleInfo Schedule, DateTime? NextRun);