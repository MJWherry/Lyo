using Lyo.Common.Core.Enums;
using Lyo.DateAndTime;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Schedule.Models;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Scheduler;

/// <summary>Places a <see cref="ScheduleDefinition" /> onto a <see cref="Calendar" /> for one month.</summary>
public static class ScheduleCalendar
{
    private const int MaxFires = 5000;

    /// <summary>Expands <paramref name="definition" /> across the week grid for <paramref name="year" /> and <paramref name="month" />.</summary>
    /// <param name="definition">Schedule to place. A disabled schedule yields the empty grid.</param>
    /// <param name="year">Gregorian year.</param>
    /// <param name="month">Gregorian month, 1–12.</param>
    /// <param name="zone">Zone that chooses civil days. Null uses the schedule zone, or UTC when the schedule has none.</param>
    /// <param name="hue">Optional hue copied onto every entry.</param>
    /// <param name="refs">Entity references copied onto every entry. The calendar stores them and does not load the entities.</param>
    /// <param name="keyScope">Optional prefix so two schedules that fire at the same instant keep distinct keys.</param>
    /// <remarks>
    /// Each fire is a point. An interval schedule also places its daily start-to-end window as a span. The entry title is the schedule description when one is set.
    /// </remarks>
    public static Calendar From(ScheduleDefinition definition, int year, int month, TimeZoneInfo? zone = null, int? hue = null, IReadOnlyList<EntityRef>? refs = null, string? keyScope = null)
    {
        ArgumentHelpers.ThrowIfNull(definition);
        var scheduleZone = definition.TimeZone ?? TimeZoneInfo.Utc;
        var displayZone = zone ?? scheduleZone;
        if (!definition.Enabled)
            return Calendar.ForMonth(year, month, displayZone);

        var title = string.IsNullOrWhiteSpace(definition.Description) ? definition.Type.ToString() : definition.Description!;
        var grid = Calendar.ForMonth(year, month, displayZone);
        var frames = new List<TimeFrame>();
        if (definition.Type == ScheduleType.Interval)
            frames.AddRange(IntervalWindows(definition, grid, scheduleZone, title, hue, refs, keyScope));

        frames.AddRange(Fires(definition, grid, displayZone, title, hue, refs, keyScope));
        return Calendar.FromTimeFrames(grid.Start, grid.End, displayZone, frames);
    }

    private static IEnumerable<TimeFrame> IntervalWindows(ScheduleDefinition definition, Calendar grid, TimeZoneInfo scheduleZone, string title, int? hue, IReadOnlyList<EntityRef>? refs, string? keyScope)
    {
        if (definition.StartTime is null || definition.EndTime is null)
            yield break;

        foreach (var date in CivilDates(grid, scheduleZone)) {
            var local = date.ToDateTime();
            if (!Matches(definition, local))
                continue;

#if NET6_0_OR_GREATER
            var start = local.Add(ToTimeSpan(definition.StartTime.Value));
            var end = local.Add(ToTimeSpan(definition.EndTime.Value));
#else
            var start = local.Add(ToTimeSpan(definition.StartTime));
            var end = local.Add(ToTimeSpan(definition.EndTime));
#endif        
            if (end < start)
                end = end.AddDays(1);

            yield return new(
                ToUtc(start, scheduleZone),
                ToUtc(end, scheduleZone),
                Scoped(keyScope, $"interval:{date}"),
                title,
                Hue: hue,
                Category: "IntervalWindow",
                Refs: refs);
        }
    }

    private static IEnumerable<TimeFrame> Fires(ScheduleDefinition definition, Calendar grid, TimeZoneInfo displayZone, string title, int? hue, IReadOnlyList<EntityRef>? refs, string? keyScope)
    {
        var rangeStart = ToUtc(grid.Start.ToDateTime(), displayZone);
        var rangeEnd = ToUtc(grid.End.AddDays(1).ToDateTime(), displayZone);
        foreach (var run in ScheduleCalculator.GetNextRuns(definition, rangeStart.AddTicks(-1), MaxFires, grid.Days.Count + 1)) {
            var utc = AsUtc(run);
            if (utc >= rangeEnd)
                yield break;

            if (utc < rangeStart)
                continue;

            yield return new(utc, utc, Scoped(keyScope, $"fire:{utc:O}"), title, Hue: hue, Category: definition.Type.ToString(), Refs: refs);
        }
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

    private static bool Matches(ScheduleDefinition definition, DateTime localDate)
    {
        var day = (DayFlags)(1 << (int)localDate.DayOfWeek);
        var month = (MonthFlags)(1 << (localDate.Month - 1));
        return definition.DayFlags.HasFlag(day) && definition.MonthFlags.HasFlag(month);
    }

    private static string Scoped(string? scope, string key) => string.IsNullOrEmpty(scope) ? key : scope + ":" + key;

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime ToUtc(DateTime local, TimeZoneInfo zone) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone);

    private static TimeSpan ToTimeSpan(TimeOnly time) => time.ToTimeSpan();
}
