using Lyo.Common.Enums;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;

#if NET6_0_OR_GREATER

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Scheduler;

/// <summary>Evaluates whether scheduled slots fall inside <see cref="JobBlackoutCalendarRes" /> do-not-run windows.</summary>
internal static class JobBlackoutCalendarEvaluator
{
    /// <summary>Adjusts a candidate slot for blackout policy. Returns null when the slot should be skipped, or the (possibly deferred) UTC slot when it may fire.</summary>
    public static DateTime? AdjustSlotForBlackout(DateTime slotUtc, JobBlackoutCalendarRes? calendar, TimeZoneInfo? timeZone)
    {
        if (calendar is null || !calendar.Enabled || calendar.BlackoutWindows is not { Count: > 0 })
            return slotUtc;

        var tz = timeZone ?? TimeZoneInfo.Utc;
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(slotUtc, DateTimeKind.Utc), tz);
        var dayFlag = GetDayFlagForDate(local);
        foreach (var window in calendar.BlackoutWindows.Where(w => w.Enabled)) {
            if (!MatchesWindowDate(window, local, dayFlag))
                continue;

            var windowStart = local.Date + window.StartTime.ToTimeSpan();
            var windowEnd = local.Date + window.EndTime.ToTimeSpan();
            if (windowEnd <= windowStart)
                windowEnd = windowEnd.AddDays(1);

            if (local < windowStart || local >= windowEnd)
                continue;

            return window.Policy switch {
                JobBlackoutPolicy.Skip => null,
                JobBlackoutPolicy.Defer => TimeZoneInfo.ConvertTimeToUtc(windowEnd, tz),
                var _ => slotUtc
            };
        }

        return slotUtc;
    }

    private static bool MatchesWindowDate(JobBlackoutWindowRes window, DateTime local, DayFlags dayFlag)
    {
        if (window.StartDateUtc.HasValue) {
            var start = window.StartDateUtc.Value.Date;
            var end = (window.EndDateUtc ?? window.StartDateUtc).Value.Date;
            var localDate = local.Date;
            return localDate >= start && localDate <= end;
        }

        return window.DayFlags.HasFlag(dayFlag);
    }

    private static DayFlags GetDayFlagForDate(DateTime date) => (DayFlags)(1 << (int)date.DayOfWeek);
}