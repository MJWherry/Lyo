namespace Lyo.Job.Scheduler;

/// <summary>Shared schedule-reference calculation for due-slot evaluation (unit-tested).</summary>
internal static class JobScheduleReference
{
    /// <summary>
    /// Picks the cursor passed to <c>ScheduleCalculator.GetNextRun</c>. Prefers last success, then last attempted slot,
    /// then schedule start / misfire lookback — never a decade-old default that traps never-run schedules.
    /// </summary>
    public static DateTime Resolve(
        DateTime? lastSuccessfulStartedUtc,
        DateTime? lastRunScheduledSlotUtc,
        DateTime? lastRunStartedUtc,
        DateTime? lastRunCreatedUtc,
        DateTime? scheduleStartDateUtc,
        DateTime nowUtc,
        int misfireLookbackMinutes)
    {
        var lastRunTime = lastSuccessfulStartedUtc
                          ?? lastRunScheduledSlotUtc
                          ?? lastRunStartedUtc
                          ?? lastRunCreatedUtc;
        if (lastRunTime.HasValue)
            return lastRunTime.Value;

        var lookbackStart = nowUtc.AddMinutes(-Math.Max(0, misfireLookbackMinutes));
        if (scheduleStartDateUtc is { } start && start > lookbackStart)
            return start;

        return lookbackStart;
    }
}
