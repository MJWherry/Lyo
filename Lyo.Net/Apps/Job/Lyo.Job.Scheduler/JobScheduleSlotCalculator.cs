using Lyo.Job.Models;
using Lyo.Job.Models.Response;
using Lyo.Schedule.Models;
using Lyo.Scheduler;
using Lyo.Exceptions;

namespace Lyo.Job.Scheduler;

/// <summary>
/// Slot math for one schedule: which slot is due now, and which slot was missed. Pure functions over an already-resolved <see cref="ScheduleDefinition" /> (time zone
/// applied), blackout calendar, and reference instant, so cron, DST, window, and blackout interactions are testable without a scheduler, an API client, or a message queue.
/// The caller owns resolution and gating (enabled flags, running runs, parallel restrictions) and the metrics/logging that go with them.
/// </summary>
public static class JobScheduleSlotCalculator
{
    /// <summary>Whether <paramref name="utcTime" /> falls inside the schedule's optional start/end bounds.</summary>
    /// <param name="schedule">Schedule carrying the bounds.</param>
    /// <param name="utcTime">Instant to test.</param>
    public static bool IsWithinWindow(JobScheduleRes schedule, DateTime utcTime)
    {
        ArgumentHelpers.ThrowIfNull(schedule);
        if (schedule.StartDateUtc.HasValue && utcTime < schedule.StartDateUtc.Value)
            return false;

        return !schedule.EndDateUtc.HasValue || utcTime <= schedule.EndDateUtc.Value;
    }

    /// <summary>
    /// The slot this schedule is due for at <paramref name="now" />, blackout-adjusted, or null when nothing is due. A due slot is always at or before <paramref name="now" />: a
    /// blackout that pushes the slot into the future defers it to a later check rather than creating a run early.
    /// </summary>
    /// <param name="definition">Schedule definition with its time zone already applied.</param>
    /// <param name="schedule">Schedule being evaluated, for its start/end bounds.</param>
    /// <param name="calendar">Blackout calendar in force, or null.</param>
    /// <param name="timeZone">Zone the blackout windows are expressed in.</param>
    /// <param name="reference">Anchor for the next-run calculation (see <see cref="JobScheduleReference" />).</param>
    /// <param name="now">Current UTC instant.</param>
    public static DateTime? GetDueSlot(
        ScheduleDefinition definition,
        JobScheduleRes schedule,
        JobBlackoutCalendarRes? calendar,
        TimeZoneInfo? timeZone,
        DateTime reference,
        DateTime now)
    {
        ArgumentHelpers.ThrowIfNull(definition);
        ArgumentHelpers.ThrowIfNull(schedule);
        var nextDue = ScheduleCalculator.GetNextRun(definition, reference);
        if (!nextDue.HasValue || nextDue.Value > now || !IsWithinWindow(schedule, nextDue.Value))
            return null;

        var adjusted = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(nextDue.Value, calendar, timeZone);
        return adjusted.HasValue && adjusted.Value <= now ? adjusted : null;
    }

    /// <summary>
    /// The most recent slot at or before <paramref name="now" /> that should have fired but did not, or null when the lookback window holds none. Slots are scanned newest-first
    /// through a doubling window: walking forward from the reference gave up on dense schedules (a five-second interval produces thousands of slots in a 24-hour lookback) and
    /// returned an older slot, or none, instead of the newest missed one.
    /// </summary>
    /// <param name="definition">Schedule definition with its time zone already applied.</param>
    /// <param name="schedule">Schedule being evaluated, for its start/end bounds.</param>
    /// <param name="calendar">Blackout calendar in force, or null.</param>
    /// <param name="timeZone">Zone the blackout windows are expressed in.</param>
    /// <param name="reference">Anchor for the next-run calculation (see <see cref="JobScheduleReference" />).</param>
    /// <param name="lookbackStart">Oldest instant a missed slot may have.</param>
    /// <param name="now">Current UTC instant.</param>
    public static DateTime? FindMostRecentMissedSlot(
        ScheduleDefinition definition,
        JobScheduleRes schedule,
        JobBlackoutCalendarRes? calendar,
        TimeZoneInfo? timeZone,
        DateTime reference,
        DateTime lookbackStart,
        DateTime now)
    {
        ArgumentHelpers.ThrowIfNull(definition);
        ArgumentHelpers.ThrowIfNull(schedule);
        var searchStart = reference < lookbackStart ? lookbackStart : reference;
        if (schedule.StartDateUtc.HasValue && schedule.StartDateUtc.Value > searchStart)
            searchStart = schedule.StartDateUtc.Value;

        if (searchStart >= now)
            return null;

        var window = TimeSpan.FromMinutes(1);
        while (true) {
            var windowStart = now - window;
            if (windowStart < searchStart)
                windowStart = searchStart;

            var found = ScanWindowForLatestSlot(definition, schedule, calendar, timeZone, windowStart, lookbackStart, now);
            if (found.HasValue)
                return found;

            if (windowStart <= searchStart)
                return null;

            window *= 2;
        }
    }

    /// <summary>Walks slots in <c>(windowStart, now]</c> and returns the latest one that is inside the schedule window and not blacked out.</summary>
    private static DateTime? ScanWindowForLatestSlot(
        ScheduleDefinition definition,
        JobScheduleRes schedule,
        JobBlackoutCalendarRes? calendar,
        TimeZoneInfo? timeZone,
        DateTime windowStart,
        DateTime lookbackStart,
        DateTime now)
    {
        DateTime? mostRecent = null;
        var cursor = windowStart;
        while (true) {
            var next = ScheduleCalculator.GetNextRun(definition, cursor);
            if (!next.HasValue || next.Value > now)
                return mostRecent;

            if (next.Value >= lookbackStart && IsWithinWindow(schedule, next.Value)) {
                var adjusted = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(next.Value, calendar, timeZone);
                if (adjusted.HasValue)
                    mostRecent = adjusted.Value;
            }

            var advanced = next.Value.AddMilliseconds(1);
            if (advanced <= cursor)
                return mostRecent; // defensive: a calculator that stops advancing would otherwise spin forever

            cursor = advanced;
        }
    }
}
