using Lyo.Common.Enums;
using Lyo.Schedule.Models;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Scheduler;

internal static class ScheduleCalculator
{
    private const int MaxDaysLookAhead = 14;

    /// <summary>Enumerates the next scheduled execution times for a schedule.</summary>
    public static IEnumerable<DateTime> GetNextRuns(ScheduleDefinition schedule, DateTime? after = null, int maxCount = 100)
    {
        if (!schedule.Enabled)
            yield break;

        var current = after ?? DateTime.UtcNow;
        var count = 0;
        while (count < maxCount) {
            var next = GetNextRun(schedule, current);
            if (!next.HasValue)
                yield break;

            yield return next.Value;

            count++;
            current = next.Value.AddMilliseconds(1);
        }
    }

    /// <summary>Gets the next scheduled execution time for a recurring schedule, or null if none.</summary>
    public static DateTime? GetNextRun(ScheduleDefinition schedule, DateTime? after = null)
    {
        if (!schedule.Enabled)
            return null;

        var now = after ?? DateTime.UtcNow;
        var localNow = GetLocalTime(schedule.TimeZone, now);
        return schedule.Type switch {
            ScheduleType.SetTimes => GetNextRunSetTimes(schedule, localNow),
            ScheduleType.Interval => GetNextRunInterval(schedule, localNow),
            ScheduleType.OneShot => GetNextRunOneShot(schedule, now),
            var _ => null
        };
    }

    /// <summary>Gets all scheduled times for a given date (for SetTimes or Interval).</summary>
    public static IEnumerable<DateTime> GetScheduledTimesForDay(ScheduleDefinition schedule, DateTime date)
    {
        var localDate = GetLocalTime(schedule.TimeZone, date);
        var dayFlag = GetDayFlagForDate(localDate);
        var monthFlag = GetMonthFlagForDate(localDate);
        if (!schedule.DayFlags.HasFlag(dayFlag) || !schedule.MonthFlags.HasFlag(monthFlag))
            yield break;

        switch (schedule.Type) {
            case ScheduleType.SetTimes when schedule.Times != null:
                foreach (var t in schedule.Times.OrderBy(x => x))
                    yield return date.Date.Add(ToTimeSpan(t));

                break;
            case ScheduleType.Interval when schedule.StartTime != null && schedule.EndTime != null && schedule.IntervalMinutes > 0:
#if NET6_0_OR_GREATER
                var currentInterval = schedule.StartTime!.Value;
                var endInterval = schedule.EndTime!.Value;
#else
                var currentInterval = schedule.StartTime!;
                var endInterval = schedule.EndTime!;
#endif
                while (CompareTime(currentInterval, endInterval) <= 0) {
                    yield return date.Date.Add(ToTimeSpan(currentInterval));

                    currentInterval = AddMinutes(currentInterval, schedule.IntervalMinutes!.Value);
                }

                break;
        }
    }

    private static DateTime? GetNextRunSetTimes(ScheduleDefinition schedule, DateTime localNow)
    {
        if (schedule.Times == null || schedule.Times.Count == 0)
            return null;

        for (var i = 0; i <= MaxDaysLookAhead; i++) {
            var targetDate = localNow.Date.AddDays(i);
            var dayFlag = GetDayFlagForDate(targetDate);
            var monthFlag = GetMonthFlagForDate(targetDate);
            if (!schedule.DayFlags.HasFlag(dayFlag) || !schedule.MonthFlags.HasFlag(monthFlag))
                continue;

            foreach (var scheduleTime in schedule.Times.OrderBy(x => x)) {
                var scheduledLocal = targetDate.Add(ToTimeSpan(scheduleTime));
                if (scheduledLocal > localNow)
                    return ToUtcIfNeeded(schedule.TimeZone, scheduledLocal);
            }
        }

        return null;
    }

    private static DateTime? GetNextRunInterval(ScheduleDefinition schedule, DateTime localNow)
    {
        if (schedule.StartTime == null || schedule.EndTime == null || schedule.IntervalMinutes <= 0)
            return null;

        for (var i = 0; i <= MaxDaysLookAhead; i++) {
            var targetDate = localNow.Date.AddDays(i);
            var dayFlag = GetDayFlagForDate(targetDate);
            var monthFlag = GetMonthFlagForDate(targetDate);
            if (!schedule.DayFlags.HasFlag(dayFlag) || !schedule.MonthFlags.HasFlag(monthFlag))
                continue;

#if NET6_0_OR_GREATER
            var currentInterval = schedule.StartTime!.Value;
            var endInterval = schedule.EndTime!.Value;
#else
            var currentInterval = schedule.StartTime!;
            var endInterval = schedule.EndTime!;
#endif
            while (CompareTime(currentInterval, endInterval) <= 0) {
                var scheduledLocal = targetDate.Add(ToTimeSpan(currentInterval));
                if (scheduledLocal > localNow)
                    return ToUtcIfNeeded(schedule.TimeZone, scheduledLocal);

                currentInterval = AddMinutes(currentInterval, schedule.IntervalMinutes!.Value);
            }
        }

        return null;
    }

    private static DateTime? GetNextRunOneShot(ScheduleDefinition schedule, DateTime now)
    {
        if (!schedule.ExecuteAt.HasValue)
            return null;

        var executeAt = schedule.ExecuteAt.Value;
        if (executeAt.Kind == DateTimeKind.Unspecified)
            executeAt = DateTime.SpecifyKind(executeAt, DateTimeKind.Utc);

        return executeAt > now ? executeAt : null;
    }

    private static DateTime GetLocalTime(TimeZoneInfo? timeZone, DateTime utcTime)
    {
        if (timeZone == null)
            return utcTime.Kind == DateTimeKind.Utc ? utcTime.ToLocalTime() : utcTime;

        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);
    }

    private static DateTime ToUtcIfNeeded(TimeZoneInfo? timeZone, DateTime localTime)
    {
        if (timeZone == null)
            return localTime.Kind == DateTimeKind.Local ? localTime.ToUniversalTime() : localTime;

        return TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone);
    }

    private static DayFlags GetDayFlagForDate(DateTime date) => (DayFlags)(1 << (int)date.DayOfWeek);

    private static MonthFlags GetMonthFlagForDate(DateTime date) => (MonthFlags)(1 << (date.Month - 1));

    private static TimeSpan ToTimeSpan(TimeOnly t)
    {
#if NET6_0_OR_GREATER
        return t.ToTimeSpan();
#else
        return t.ToTimeSpan();
#endif
    }

    private static int CompareTime(TimeOnly a, TimeOnly b)
    {
#if NET6_0_OR_GREATER
        return a.CompareTo(b);
#else
        return a.Ticks.CompareTo(b.Ticks);
#endif
    }

    private static TimeOnly AddMinutes(TimeOnly t, int minutes)
    {
#if NET6_0_OR_GREATER
        return t.AddMinutes(minutes);
#else
        return (TimeOnly)t.AddMinutes(minutes);
#endif
    }
}