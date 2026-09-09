using Lyo.Common.Core.Enums;
using Lyo.DateAndTime;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Models;

/// <summary>
/// Checks whether scheduled slots fall inside <see cref="JobBlackoutCalendarRes" /> do-not-run windows. Shared by the job scheduler and
/// <c>GetNextRuns</c> so weekday, dated-range, calendar-day, and holiday windows are evaluated the same way.
/// </summary>
public static class JobBlackoutCalendarEvaluator
{
    /// <summary>
    /// Applies blackout policy to a candidate slot. Returns null when the slot should be skipped, or the (possibly deferred) UTC slot when it may fire.
    /// </summary>
    /// <param name="slotUtc">Candidate fire time in UTC.</param>
    /// <param name="calendar">Calendar whose enabled windows are considered, or null when none is attached.</param>
    /// <param name="timeZone">Zone the window clock times are expressed in. Null uses UTC.</param>
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
        if (!string.IsNullOrWhiteSpace(window.HolidaySlug)) {
            var holiday = HolidayInfo.FromSlug(window.HolidaySlug);
            if (ReferenceEquals(holiday, HolidayInfo.Unknown))
                return false;

            return holiday.OccursOn(local, window.IncludeObservedDate);
        }

        if (window.DaysOfMonth is { Count: > 0 }) {
            var months = window.MonthFlags ?? MonthFlags.None;
            if (!months.HasFlag(GetMonthFlagForDate(local)) || !window.DaysOfMonth.Contains(local.Day))
                return false;

            if (!window.StartDateUtc.HasValue)
                return true;

            return InDateRange(window, local);
        }

        if (window.StartDateUtc.HasValue)
            return InDateRange(window, local);

        return window.DayFlags.HasFlag(dayFlag);
    }

    private static bool InDateRange(JobBlackoutWindowRes window, DateTime local)
    {
        var start = window.StartDateUtc!.Value.Date;
        var end = (window.EndDateUtc ?? window.StartDateUtc).Value.Date;
        var localDate = local.Date;
        return localDate >= start && localDate <= end;
    }

    private static DayFlags GetDayFlagForDate(DateTime date) => (DayFlags)(1 << (int)date.DayOfWeek);

    private static MonthFlags GetMonthFlagForDate(DateTime date) => (MonthFlags)(1 << (date.Month - 1));
}
