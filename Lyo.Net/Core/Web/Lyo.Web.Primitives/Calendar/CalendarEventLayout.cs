using System.Diagnostics;
using Lyo.DateAndTime;
using Lyo.Exceptions;

namespace Lyo.Web.Primitives;

/// <summary>Places entries on one civil day. Overlapping timed entries sit in side-by-side lanes. A span that covers the day is all-day.</summary>
internal static class CalendarEventLayout
{
    private const double MinutesPerDay = 1440;
    private const double PointMinutes = 30;
    private const double AllDayMinutes = 18 * 60;

    /// <summary>Places each UTC span on <paramref name="day" /> in <paramref name="zone" />. Input order is preserved on <see cref="Block.Index" />.</summary>
    public static IReadOnlyList<Block> Place(IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> entries, DateOnlyModel day, TimeZoneInfo zone)
    {
        ArgumentHelpers.ThrowIfNull(entries);
        ArgumentHelpers.ThrowIfNull(day);
        ArgumentHelpers.ThrowIfNull(zone);
        var dayStart = day.ToDateTime();
        var dayEnd = dayStart.AddDays(1);
        var blocks = new Block[entries.Count];
        var timed = new List<Timed>();
        for (var i = 0; i < entries.Count; i++) {
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(AsUtc(entries[i].StartUtc), zone);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(AsUtc(entries[i].EndUtc), zone);
            var clipStart = localStart < dayStart ? dayStart : localStart;
            var clipEnd = localEnd > dayEnd ? dayEnd : localEnd;
            if (clipEnd < clipStart)
                clipEnd = clipStart;

            var startMin = (clipStart - dayStart).TotalMinutes;
            var endMin = (clipEnd - dayStart).TotalMinutes;
            var coversDay = localStart <= dayStart && localEnd >= dayEnd;
            if (coversDay || endMin - startMin >= AllDayMinutes) {
                blocks[i] = new(i, 0, 1, 0, 100, true);
                continue;
            }

            if (endMin <= startMin)
                endMin = Math.Min(MinutesPerDay, startMin + PointMinutes);

            timed.Add(new(i, startMin, endMin));
        }

        AssignLanes(timed, blocks);
        return blocks;
    }

    private static void AssignLanes(List<Timed> timed, Block[] blocks)
    {
        timed.Sort(static (a, b) => {
            var byStart = a.Start.CompareTo(b.Start);
            return byStart != 0 ? byStart : a.End.CompareTo(b.End);
        });

        var index = 0;
        while (index < timed.Count) {
            var cluster = new List<Timed>();
            var clusterEnd = timed[index].End;
            var next = index;
            while (next < timed.Count && timed[next].Start < clusterEnd) {
                cluster.Add(timed[next]);
                if (timed[next].End > clusterEnd)
                    clusterEnd = timed[next].End;

                next++;
            }

            var laneEnds = new List<double>();
            var placed = new List<(Timed Item, int Lane)>();
            foreach (var item in cluster) {
                var lane = -1;
                for (var i = 0; i < laneEnds.Count; i++) {
                    if (laneEnds[i] <= item.Start) {
                        lane = i;
                        break;
                    }
                }

                if (lane < 0) {
                    lane = laneEnds.Count;
                    laneEnds.Add(item.End);
                }
                else
                    laneEnds[lane] = item.End;

                placed.Add((item, lane));
            }

            var laneCount = laneEnds.Count;
            foreach (var (item, lane) in placed) {
                var top = item.Start / MinutesPerDay * 100d;
                var height = (item.End - item.Start) / MinutesPerDay * 100d;
                blocks[item.Index] = new(item.Index, lane, laneCount, top, height, false);
            }

            index = next;
        }
    }

    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    /// <summary>One placed block. <see cref="Index" /> matches the input list. <see cref="AllDay" /> blocks ignore lane and percent fields.</summary>
    /// <param name="Index">Index into the input list.</param>
    /// <param name="Lane">Zero-based column among events that overlap this one.</param>
    /// <param name="LaneCount">Columns in this event's overlap group.</param>
    /// <param name="TopPercent">Distance from local midnight, as a percent of the day.</param>
    /// <param name="HeightPercent">Visual height as a percent of the day. Points use a short block so they stay readable.</param>
    /// <param name="AllDay">True when the entry covers the civil day and should sit above the time grid.</param>
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly record struct Block(int Index, int Lane, int LaneCount, double TopPercent, double HeightPercent, bool AllDay);

    private sealed record Timed(int Index, double Start, double End);
}