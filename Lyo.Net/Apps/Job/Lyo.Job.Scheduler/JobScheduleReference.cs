namespace Lyo.Job.Scheduler;

/// <summary>Shared schedule-reference math for due-slot evaluation (covered by unit tests).</summary>
internal static class JobScheduleReference
{
    /// <summary>
    /// Picks the cursor passed to <c>ScheduleCalculator.GetNextRun</c>. Uses the later of last success and last attempted slot for this schedule, then last run start/created,
    /// then schedule start / misfire lookback. Preferring last success alone re-proposed a slot that already had a failed run and 500-looped on the unique constraint. Never a
    /// decade-old default that traps never-run schedules.
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
        var lastRunTime = Later(lastSuccessfulStartedUtc, lastRunScheduledSlotUtc) ?? lastRunStartedUtc ?? lastRunCreatedUtc;
        if (lastRunTime.HasValue)
            return lastRunTime.Value;

        var lookbackStart = nowUtc.AddMinutes(-Math.Max(0, misfireLookbackMinutes));
        if (scheduleStartDateUtc is { } start && start > lookbackStart)
            return start;

        return lookbackStart;
    }

    private static DateTime? Later(DateTime? left, DateTime? right)
    {
        if (!left.HasValue)
            return right;
        if (!right.HasValue)
            return left;

        return left.Value >= right.Value ? left : right;
    }
}
