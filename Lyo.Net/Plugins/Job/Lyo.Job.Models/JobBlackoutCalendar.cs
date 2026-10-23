#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif
using Lyo.Common.Core.Enums;
using Lyo.DateAndTime;
using Lyo.Exceptions;
using Lyo.Job.Models.Response;
using Lyo.Schedule.Models;

namespace Lyo.Job.Models;

/// <summary>Places enabled blackout windows onto a <see cref="Calendar" />. The window name becomes the entry title.</summary>
public static class JobBlackoutCalendar
{
    /// <summary>Expands <paramref name="calendar" /> across the week grid for <paramref name="year" /> and <paramref name="month" />.</summary>
    /// <param name="calendar">Blackout calendar whose enabled windows are placed. A disabled calendar yields the empty grid.</param>
    /// <param name="year">Gregorian year.</param>
    /// <param name="month">Gregorian month, 1–12.</param>
    /// <param name="zone">Zone the window clocks are expressed in, and the zone that chooses civil days. Null uses UTC.</param>
    public static Calendar ToCalendar(JobBlackoutCalendarRes calendar, int year, int month, TimeZoneInfo? zone = null)
    {
        ArgumentHelpers.ThrowIfNull(calendar);
        var display = zone ?? TimeZoneInfo.Utc;
        var grid = Calendar.ForMonth(year, month, display);
        if (!calendar.Enabled || calendar.BlackoutWindows is not { Count: > 0 })
            return grid;

        var frames = new List<TimeFrame>();
        foreach (var window in calendar.BlackoutWindows.Where(item => item.Enabled)) {
            foreach (var date in CivilDates(grid, display)) {
                var local = date.ToDateTime();
                if (!JobBlackoutCalendarEvaluator.MatchesWindowDate(window, local, DayFlag(local)))
                    continue;

                var start = local.Add(ToTimeSpan(window.StartTime));
                var end = local.Add(ToTimeSpan(window.EndTime));
                if (end <= start)
                    end = end.AddDays(1);

                frames.Add(new(ToUtc(start, display), ToUtc(end, display), $"{window.Id:N}:{date}", window.Name, Category: "blackout"));
            }
        }

        return Calendar.FromTimeFrames(grid.Start, grid.End, display, frames);
    }

    /// <summary>
    /// Marks fires <see cref="JobBlackoutCalendarEvaluator" /> would skip as <see cref="CalendarEntry.Muted" />, and moves a deferred fire to the end of its window. Blackout
    /// spans are not added here.
    /// </summary>
    /// <param name="schedule">Placed schedule. A disabled blackout returns this calendar unchanged.</param>
    /// <param name="blackout">Calendar whose enabled windows are applied. Window clocks are in <paramref name="zone" />.</param>
    /// <param name="zone">Zone the window clocks use. Null uses UTC.</param>
    public static Calendar Suppress(Calendar schedule, JobBlackoutCalendarRes blackout, TimeZoneInfo? zone = null)
    {
        ArgumentHelpers.ThrowIfNull(schedule);
        ArgumentHelpers.ThrowIfNull(blackout);
        if (!blackout.Enabled || blackout.BlackoutWindows is not { Count: > 0 })
            return schedule;

        var clock = zone ?? TimeZoneInfo.Utc;
        var kept = new List<CalendarEntry>();
        foreach (var entry in schedule.Entries) {
            var start = entry.Start.Kind == DateTimeKind.Utc ? entry.Start : DateTime.SpecifyKind(entry.Start, DateTimeKind.Utc);
            var adjusted = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(start, blackout, clock);
            if (adjusted is null) {
                kept.Add(entry with { Muted = true });
                continue;
            }

            if (entry.IsPoint && adjusted.Value != start) {
                var at = DateTime.SpecifyKind(adjusted.Value, DateTimeKind.Utc);
                kept.Add(entry with { Start = at, End = at, Key = entry.Key + ":deferred" });
                continue;
            }

            kept.Add(entry);
        }

        return new(schedule.Start, schedule.End, schedule.TimeZone, kept);
    }

    private static IEnumerable<DateOnlyModel> CivilDates(Calendar grid, TimeZoneInfo zone)
    {
        var firstInstant = ToUtc(grid.Start.ToDateTime(), grid.TimeZone);
        var lastInstant = ToUtc(grid.End.AddDays(1).ToDateTime(), grid.TimeZone).AddTicks(-1);
        var first = DateOnlyModel.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(firstInstant, zone))!;
        var last = DateOnlyModel.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(lastInstant, zone))!;
        for (var date = first; date <= last; date = date.AddDays(1))
            yield return date;
    }

    private static DayFlags DayFlag(DateTime date) => (DayFlags)(1 << (int)date.DayOfWeek);

    private static DateTime ToUtc(DateTime local, TimeZoneInfo zone) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone);

    private static TimeSpan ToTimeSpan(TimeOnly time) => time.ToTimeSpan();
}