using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.Schedule.Models;

/// <summary>Formats UTC instants for a calendar view. The hour cycle changes the label and leaves the instant on its civil day.</summary>
public static class CalendarClock
{
    /// <summary>Formats <paramref name="utc" /> in <paramref name="zone" /> as <c>22:00</c> or <c>10:00 PM</c>.</summary>
    /// <param name="utc">Instant to format. Unspecified values are treated as UTC.</param>
    /// <param name="zone">Zone whose civil clock supplies the hour and minute.</param>
    /// <param name="use24Hour">True for a 24-hour clock. False for a 12-hour clock with an AM/PM marker.</param>
    public static string Format(DateTime utc, TimeZoneInfo zone, bool use24Hour)
    {
        ArgumentHelpers.ThrowIfNull(zone);
        var local = TimeZoneInfo.ConvertTimeFromUtc(CalendarEntry.AsUtc(utc), zone);
        var pattern = use24Hour ? "HH:mm" : "h:mm tt";
        return local.ToString(pattern, CultureInfo.InvariantCulture);
    }

    /// <summary>Formats the zone's offset at <paramref name="utc" /> as <c>UTC-04:00</c> or <c>UTC+00:00</c>.</summary>
    /// <param name="zone">Zone whose offset is shown in the calendar toolbar.</param>
    /// <param name="utc">Instant used to pick the offset, so daylight saving is the one in force then.</param>
    public static string FormatOffset(TimeZoneInfo zone, DateTime utc)
    {
        ArgumentHelpers.ThrowIfNull(zone);
        var offset = zone.GetUtcOffset(CalendarEntry.AsUtc(utc));
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var abs = offset.Duration();
        return $"UTC{sign}{abs.Hours:00}:{abs.Minutes:00}";
    }
}
