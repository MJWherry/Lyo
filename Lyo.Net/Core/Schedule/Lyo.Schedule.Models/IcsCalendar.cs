using System.Globalization;
using System.Text;
using Lyo.Exceptions;

namespace Lyo.Schedule.Models;

/// <summary>Reads and writes one <c>VEVENT</c> as a <see cref="CalendarEntry" />. Recurrence expansion stops at a caller window and at 366 occurrences.</summary>
public sealed class IcsCalendar
{
    /// <summary>Hard cap for <see cref="Expand" />. More than this is an error.</summary>
    public const int MaxOccurrences = 366;

    /// <summary>Reads the first <c>VEVENT</c>. An all-day <c>DATE</c> is a UTC midnight span ending the next midnight when <c>DTEND</c> is absent.</summary>
    public CalendarEntry Read(string ics)
    {
        ArgumentHelpers.ThrowIfNull(ics);
        var fields = ReadEvent(ics);
        var start = ParseStamp(Required(fields, "DTSTART"), out var allDay);
        var end = fields.TryGetValue("DTEND", out var endText)
            ? ParseStamp(endText, out _)
            : allDay ? start.AddDays(1) : start;
        return new(
            start,
            end,
            Required(fields, "UID"),
            Required(fields, "SUMMARY"),
            fields.TryGetValue("DESCRIPTION", out var description) ? description : null);
    }

    /// <summary>Writes one timed or all-day event. All-day entries use <c>VALUE=DATE</c>.</summary>
    public string Write(CalendarEntry entry)
    {
        ArgumentHelpers.ThrowIfNull(entry);
        var builder = new StringBuilder();
        builder.Append("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\n");
        builder.Append("UID:").Append(Escape(entry.Key)).Append("\r\n");
        builder.Append("SUMMARY:").Append(Escape(entry.Title)).Append("\r\n");
        if (!string.IsNullOrEmpty(entry.Description))
            builder.Append("DESCRIPTION:").Append(Escape(entry.Description!)).Append("\r\n");
        AppendStamp(builder, "DTSTART", entry.Start);
        AppendStamp(builder, "DTEND", entry.End);
        builder.Append("END:VEVENT\r\nEND:VCALENDAR\r\n");
        return builder.ToString();
    }

    /// <summary>
    /// Expands one <c>RRULE</c> of daily, weekly, or monthly inside <paramref name="until" /> or <paramref name="count" />.
    /// More than <see cref="MaxOccurrences" /> throws <see cref="IcsCalendarException" /> instead of returning a shorter list.
    /// </summary>
    public IReadOnlyList<CalendarEntry> Expand(string ics, DateTime? until, int? count)
    {
        ArgumentHelpers.ThrowIfNull(ics);
        if (until is null && count is null)
            throw new IcsCalendarException("Expansion requires until or count.");
        if (count is < 1)
            throw new IcsCalendarException("Count must be at least 1.");

        var fields = ReadEvent(ics);
        if (!fields.TryGetValue("RRULE", out var ruleText))
            return [Read(ics)];

        var rule = ParseRule(ruleText);
        var seed = Read(ics);
        var duration = seed.End - seed.Start;
        var cursor = seed.Start;
        var list = new List<CalendarEntry>();
        var limit = count ?? int.MaxValue;
        var window = until is null ? DateTime.MaxValue : DateTime.SpecifyKind(until.Value, DateTimeKind.Utc);
        while (list.Count < limit && cursor <= window) {
            if (list.Count >= MaxOccurrences)
                throw new IcsCalendarException($"Expansion exceeds {MaxOccurrences} occurrences.");

            list.Add(seed with { Start = cursor, End = cursor + duration });
            cursor = rule.Next(cursor);
        }

        if (count is > MaxOccurrences && list.Count >= MaxOccurrences)
            throw new IcsCalendarException($"Expansion exceeds {MaxOccurrences} occurrences.");

        return list;
    }

    private static Dictionary<string, string> ReadEvent(string ics)
    {
        var lines = Unfold(ics);
        var inside = false;
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines) {
            if (line.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase)) {
                inside = true;
                continue;
            }

            if (line.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
                break;
            if (!inside)
                continue;

            var colon = line.IndexOf(':');
            if (colon <= 0)
                continue;
            var name = line.Substring(0, colon);
            var semi = name.IndexOf(';');
            if (semi >= 0)
                name = name.Substring(0, semi);
            fields[name] = line.Substring(colon + 1);
        }

        if (fields.Count == 0)
            throw new IcsCalendarException("Calendar has no VEVENT.");
        return fields;
    }

    private static List<string> Unfold(string ics)
    {
        var raw = ics.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var lines = new List<string>();
        foreach (var rawLine in raw) {
            if (rawLine.Length == 0)
                continue;
            if ((rawLine[0] == ' ' || rawLine[0] == '\t') && lines.Count > 0)
                lines[lines.Count - 1] += rawLine.Substring(1);
            else
                lines.Add(rawLine);
        }

        return lines;
    }

    private static string Required(Dictionary<string, string> fields, string name)
        => fields.TryGetValue(name, out var value) && value.Length > 0
            ? value
            : throw new IcsCalendarException($"VEVENT is missing {name}.");

    private static DateTime ParseStamp(string text, out bool allDay)
    {
        if (text.Length == 8 && DateTime.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date)) {
            allDay = true;
            return date;
        }

        allDay = false;
        var format = text.EndsWith("Z", StringComparison.Ordinal) ? "yyyyMMdd'T'HHmmss'Z'" : "yyyyMMdd'T'HHmmss";
        if (!DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var instant))
            throw new IcsCalendarException($"Cannot read '{text}' as a calendar time.");
        return instant;
    }

    private static void AppendStamp(StringBuilder builder, string name, DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        builder.Append(name).Append(':').Append(utc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)).Append("\r\n");
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\n", "\\n").Replace(",", "\\,");

    private static Rule ParseRule(string text)
    {
        string? freq = null;
        foreach (var part in text.Split(';')) {
            var eq = part.IndexOf('=');
            if (eq <= 0)
                continue;
            var key = part.Substring(0, eq).Trim();
            var val = part.Substring(eq + 1).Trim();
            if (key.Equals("FREQ", StringComparison.OrdinalIgnoreCase))
                freq = val;
        }

        return freq?.ToUpperInvariant() switch {
            "DAILY" => new Rule(date => date.AddDays(1)),
            "WEEKLY" => new Rule(date => date.AddDays(7)),
            "MONTHLY" => new Rule(date => date.AddMonths(1)),
            _ => throw new IcsCalendarException("RRULE must be daily, weekly, or monthly.")
        };
    }

    private readonly record struct Rule(Func<DateTime, DateTime> Next);
}
