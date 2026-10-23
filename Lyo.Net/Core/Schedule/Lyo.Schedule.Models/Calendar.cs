using Lyo.DateAndTime;
using Lyo.Exceptions;

namespace Lyo.Schedule.Models;

/// <summary>A placed civil range. Entries stay UTC. Days are the civil dates in <see cref="TimeZone" /> that the range covers, including days with no entries.</summary>
public sealed class Calendar
{
    /// <summary>First civil day, inclusive.</summary>
    public DateOnlyModel Start { get; }

    /// <summary>Last civil day, inclusive.</summary>
    public DateOnlyModel End { get; }

    /// <summary>Zone used to place instants onto civil days.</summary>
    public TimeZoneInfo TimeZone { get; }

    /// <summary>Entries that intersect <see cref="Start" /> through <see cref="End" /> in <see cref="TimeZone" />.</summary>
    public IReadOnlyList<CalendarEntry> Entries { get; }

    /// <summary>Every civil day in the range, in order, including empty days.</summary>
    public IReadOnlyList<CalendarDay> Days { get; }

    /// <summary>Builds a calendar and drops entries that miss the range in <paramref name="timeZone" />.</summary>
    /// <param name="start">First civil day, inclusive.</param>
    /// <param name="end">Last civil day, inclusive.</param>
    /// <param name="timeZone">Zone used to choose which civil day an instant lands on.</param>
    /// <param name="entries">Candidate entries. Ones outside the range are omitted.</param>
    public Calendar(DateOnlyModel start, DateOnlyModel end, TimeZoneInfo timeZone, IEnumerable<CalendarEntry> entries)
    {
        ArgumentHelpers.ThrowIfNull(start);
        ArgumentHelpers.ThrowIfNull(end);
        ArgumentHelpers.ThrowIfNull(timeZone);
        ArgumentHelpers.ThrowIfNull(entries);
        ArgumentHelpers.ThrowIf(end < start, "End must be on or after Start.", nameof(end));
        Start = start;
        End = end;
        TimeZone = timeZone;
        Entries = entries.Where(entry => IntersectsRange(entry, start, end, timeZone)).ToArray();
        Days = BuildDays();
    }

    /// <summary>Places <paramref name="frames" /> onto the inclusive civil range.</summary>
    /// <param name="start">First civil day, inclusive.</param>
    /// <param name="end">Last civil day, inclusive.</param>
    /// <param name="timeZone">Zone used to choose which civil day an instant lands on.</param>
    /// <param name="frames">Spans to place. A span that misses the range is dropped. A span that crosses local midnight is listed on each day it touches.</param>
    public static Calendar FromTimeFrames(DateOnlyModel start, DateOnlyModel end, TimeZoneInfo timeZone, IEnumerable<TimeFrame> frames)
    {
        ArgumentHelpers.ThrowIfNull(frames);
        return new(start, end, timeZone, frames.Select(frame => frame.ToEntry()));
    }

    /// <summary>Builds the week grid that contains <paramref name="year" /> and <paramref name="month" />, padded so every week is full.</summary>
    /// <param name="year">Gregorian year.</param>
    /// <param name="month">Gregorian month, 1–12.</param>
    /// <param name="timeZone">Zone used to place entries. The grid itself is a civil month and does not depend on the zone.</param>
    /// <param name="firstDayOfWeek">Weekday that begins each row. Sunday matches the US civil calendar.</param>
    /// <param name="frames">Optional spans placed on the padded grid.</param>
    public static Calendar ForMonth(int year, int month, TimeZoneInfo timeZone, DayOfWeek firstDayOfWeek = DayOfWeek.Sunday, IEnumerable<TimeFrame>? frames = null)
    {
        ArgumentHelpers.ThrowIfNull(timeZone);
        var first = new DateOnlyModel(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var gridStart = first.AddDays(-LeadingPad(first, firstDayOfWeek));
        var gridEnd = last.AddDays(TrailingPad(last, firstDayOfWeek));
        return FromTimeFrames(gridStart, gridEnd, timeZone, frames ?? []);
    }

    /// <summary>Returns a calendar with the same range and UTC entries, rebucketed into <paramref name="timeZone" />.</summary>
    /// <param name="timeZone">Zone that chooses the civil day for each instant.</param>
    public Calendar WithTimeZone(TimeZoneInfo timeZone)
    {
        ArgumentHelpers.ThrowIfNull(timeZone);
        return new(Start, End, timeZone, Entries);
    }

    /// <summary>Unions entries from calendars that share a range and zone.</summary>
    /// <param name="other">Calendar whose entries are appended. Its range and zone must match this one.</param>
    public Calendar Merge(Calendar other)
    {
        ArgumentHelpers.ThrowIfNull(other);
        ArgumentHelpers.ThrowIf(Start != other.Start || End != other.End, "Calendars must share a range.", nameof(other));
        ArgumentHelpers.ThrowIf(!string.Equals(TimeZone.Id, other.TimeZone.Id, StringComparison.Ordinal), "Calendars must share a time zone.", nameof(other));
        return new(Start, End, TimeZone, Entries.Concat(other.Entries));
    }

    private IReadOnlyList<CalendarDay> BuildDays()
    {
        var days = new List<CalendarDay>();
        for (var date = Start; date <= End; date = date.AddDays(1)) {
            var onDay = Entries.Where(entry => IntersectsDay(entry, date, TimeZone))
                .OrderBy(entry => CalendarEntry.AsUtc(entry.Start))
                .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                .ToArray();

            days.Add(new(date, onDay));
        }

        return days;
    }

    private static int LeadingPad(DateOnlyModel firstOfMonth, DayOfWeek firstDayOfWeek)
    {
        var startDow = (int)firstOfMonth.ToDateTime().DayOfWeek;
        return (startDow - (int)firstDayOfWeek + 7) % 7;
    }

    private static int TrailingPad(DateOnlyModel lastOfMonth, DayOfWeek firstDayOfWeek)
    {
        var endDow = (int)lastOfMonth.ToDateTime().DayOfWeek;
        var lastDowOfWeek = ((int)firstDayOfWeek + 6) % 7;
        return (lastDowOfWeek - endDow + 7) % 7;
    }

    private static bool IntersectsRange(CalendarEntry entry, DateOnlyModel start, DateOnlyModel end, TimeZoneInfo zone)
    {
        var localStart = ToLocal(entry.Start, zone);
        var localEnd = ToLocal(entry.End, zone);
        var rangeStart = start.ToDateTime();
        var rangeEnd = end.AddDays(1).ToDateTime();
        if (localStart == localEnd)
            return localStart >= rangeStart && localStart < rangeEnd;

        return localStart < rangeEnd && localEnd > rangeStart;
    }

    private static bool IntersectsDay(CalendarEntry entry, DateOnlyModel day, TimeZoneInfo zone) => IntersectsRange(entry, day, day, zone);

    private static DateTime ToLocal(DateTime utc, TimeZoneInfo zone) => TimeZoneInfo.ConvertTimeFromUtc(CalendarEntry.AsUtc(utc), zone);
}