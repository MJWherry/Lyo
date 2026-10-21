using Lyo.DateAndTime;
using Lyo.Exceptions;

namespace Lyo.Web.Primitives;

/// <summary>Collapses one civil day into a few month-cell rows. Fires in the same local hour share a row. All-day spans lead.</summary>
internal static class CalendarMonthSummary
{
    public const int MaxRows = 3;

    /// <summary>Groups <paramref name="entries" /> on <paramref name="day" /> in <paramref name="zone" />.</summary>
    public static Result Build(IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> entries, DateOnlyModel day, TimeZoneInfo zone)
    {
        ArgumentHelpers.ThrowIfNull(entries);
        ArgumentHelpers.ThrowIfNull(day);
        ArgumentHelpers.ThrowIfNull(zone);
        var placed = CalendarEventLayout.Place(entries, day, zone);
        var allDay = new List<int>();
        var byHour = new SortedDictionary<int, List<int>>();
        for (var i = 0; i < placed.Count; i++) {
            if (placed[i].AllDay) {
                allDay.Add(i);
                continue;
            }

            var hour = TimeZoneInfo.ConvertTimeFromUtc(AsUtc(entries[i].StartUtc), zone).Hour;
            if (!byHour.TryGetValue(hour, out var bucket)) {
                bucket = [];
                byHour[hour] = bucket;
            }

            bucket.Add(i);
        }

        var rows = new List<Row>();
        if (allDay.Count > 0)
            rows.Add(new(true, -1, allDay));

        foreach (var pair in byHour)
            rows.Add(new(false, pair.Key, pair.Value));

        if (rows.Count <= MaxRows)
            return new(rows, 0);

        return new(rows.Take(MaxRows).ToArray(), rows.Count - MaxRows);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    /// <summary>One visible row. <see cref="Indexes" /> point at the input list. <see cref="Hour" /> is -1 for an all-day row.</summary>
    /// <param name="AllDay">True when every index covers the civil day.</param>
    /// <param name="Hour">Local hour 0–23, or -1 for the all-day row.</param>
    /// <param name="Indexes">Input indexes in this row.</param>
    public readonly record struct Row(bool AllDay, int Hour, IReadOnlyList<int> Indexes);

    /// <summary>The rows that fit in a month cell, plus how many hour groups were left off.</summary>
    /// <param name="Rows">At most <see cref="MaxRows" /> rows.</param>
    /// <param name="MoreRows">Hour groups and the all-day row that did not fit.</param>
    public readonly record struct Result(IReadOnlyList<Row> Rows, int MoreRows);
}