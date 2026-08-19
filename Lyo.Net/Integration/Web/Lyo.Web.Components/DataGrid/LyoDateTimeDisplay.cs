using System.Globalization;
using System.Text.Json;
using Lyo.Common.Conversion;
using Lyo.Exceptions;

namespace Lyo.Web.Components.DataGrid;

/// <summary>Formats projected timestamps for grid cells (UTC ISO → a given IANA zone, typically the browser).</summary>
public static class LyoDateTimeDisplay
{
    /// <summary>Local general date/time (<c>g</c>) in UTC when no zone is supplied. Prefer <see cref="LyoTimestamp" /> in UI.</summary>
    public static string FormatProjected(object? _, object? value) => Format(value);

    /// <summary>Latest timestamp in a projected collection, formatted like <see cref="FormatProjected" />.</summary>
    public static string FormatLatestProjected(object? _, object? value) => Format(Latest(value));

    /// <summary>Formats <paramref name="value" /> in <see cref="TimeZoneInfo.Utc" />.</summary>
    public static string Format(object? value) => Format(value, TimeZoneInfo.Utc);

    /// <summary>Formats a UTC instant in <paramref name="timeZone" /> using culture general date/time (<c>g</c>).</summary>
    public static string Format(object? value, TimeZoneInfo timeZone)
    {
        ArgumentHelpers.ThrowIfNull(timeZone);
        var utc = ToUtc(value);
        if (utc is null)
            return "—";

        return TimeZoneInfo.ConvertTimeFromUtc(utc.Value, timeZone).ToString("g", CultureInfo.CurrentCulture);
    }

    /// <summary>Picks the display string for <see cref="LyoTimestampKind" /> from a UTC instant. Relative kinds use <paramref name="relativeWindow" /> as a ± bound (default 24 hours).</summary>
    public static string FormatKind(LyoTimestampKind kind, DateTime utc, string absolute, TimeSpan? relativeWindow = null)
    {
        var window = relativeWindow ?? TimeSpan.FromHours(24);
        if (window < TimeSpan.Zero)
            window = TimeSpan.Zero;

        var delta = utc - DateTime.UtcNow;
        return kind switch {
            LyoTimestampKind.Relative when TryFormatRelative(delta, out var relative, window) => relative,
            LyoTimestampKind.TimeUntil when delta >= TimeSpan.Zero && TryFormatRelative(delta, out var until, window) => until,
            LyoTimestampKind.TimeSince when delta <= TimeSpan.Zero && TryFormatRelative(delta, out var since, window) => since,
            var _ => absolute
        };
    }

    /// <summary>Compact relative text when the absolute delta is within <paramref name="window" />; otherwise returns false.</summary>
    public static bool TryFormatRelative(TimeSpan delta, out string text, TimeSpan? window = null)
    {
        var limit = window ?? TimeSpan.FromHours(24);
        if (limit < TimeSpan.Zero)
            limit = TimeSpan.Zero;

        var past = delta < TimeSpan.Zero;
        var abs = delta.Duration();
        if (abs > limit) {
            text = "";
            return false;
        }

        if (abs < TimeSpan.FromMinutes(1)) {
            text = "now";
            return true;
        }

        var hours = (int)abs.TotalHours;
        var minutes = abs.Minutes;
        var body = hours > 0 && minutes > 0 ? $"{hours}h {minutes}m" : hours > 0 ? $"{hours}h" : $"{minutes}m";
        text = past ? $"{body} ago" : $"in {body}";
        return true;
    }

    /// <summary>
    /// Short zone label for a converted local instant (EST/EDT when the OS name is already short, otherwise initials of the English
    /// standard/daylight name). Never the IANA id. UTC stays <c>UTC</c>; unknown names fall back to a UTC offset (<c>UTC-4</c>).
    /// </summary>
    public static string FormatTimeZoneLabel(TimeZoneInfo timeZone, DateTime local)
    {
        ArgumentHelpers.ThrowIfNull(timeZone);
        if (IsUtcZone(timeZone))
            return "UTC";

        var name = timeZone.IsDaylightSavingTime(local) ? timeZone.DaylightName : timeZone.StandardName;
        if (IsShortAbbreviation(name))
            return name.Trim();

        if (TryAcronym(name, out var acronym))
            return acronym;

        return FormatUtcOffset(timeZone.GetUtcOffset(local));
    }

    private static bool IsUtcZone(TimeZoneInfo timeZone)
        => timeZone.Equals(TimeZoneInfo.Utc)
           || timeZone.Id.Equals("UTC", StringComparison.OrdinalIgnoreCase)
           || timeZone.Id.Equals("Etc/UTC", StringComparison.OrdinalIgnoreCase)
           || timeZone.Id.Equals("Etc/Universal", StringComparison.OrdinalIgnoreCase);

    private static bool IsShortAbbreviation(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var text = name.Trim();
        if (text.Length is < 2 or > 5)
            return false;

        foreach (var c in text) {
            if (!char.IsLetter(c))
                return false;
        }

        return true;
    }

    private static bool TryAcronym(string? name, out string acronym)
    {
        acronym = "";
        if (string.IsNullOrWhiteSpace(name) || name.Contains('/'))
            return false;

        var parts = name.Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;

        var letters = new char[parts.Length];
        for (var i = 0; i < parts.Length; i++) {
            var part = parts[i];
            if (!char.IsLetter(part[0]))
                return false;

            letters[i] = char.ToUpperInvariant(part[0]);
        }

        acronym = new string(letters);
        return acronym.Length is >= 2 and <= 5;
    }

    private static string FormatUtcOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var abs = offset.Duration();
        return abs.Minutes == 0 ? $"UTC{sign}{(int)abs.TotalHours}" : $"UTC{sign}{abs.Hours}:{abs.Minutes:D2}";
    }

    /// <summary>Parses a projected value to UTC. Unspecified kind is treated as UTC (API payloads).</summary>
    public static DateTime? ToUtc(object? value)
    {
        var dt = ToDateTime(value);
        if (dt is null)
            return null;

        var v = dt.Value;
        if (v.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(v, DateTimeKind.Utc);

        return v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime();
    }

    /// <summary>Newest timestamp in a scalar, JSON array, or collection of projected values.</summary>
    public static DateTime? Latest(object? value)
    {
        DateTime? best = null;
        foreach (var item in Enumerate(value)) {
            var dt = ToUtc(item);
            if (dt is null)
                continue;

            if (best is null || dt.Value > best.Value)
                best = dt;
        }

        return best;
    }

    /// <summary>Parses a projected JSON/CLR value to UTC <see cref="DateTime" />.</summary>
    public static DateTime? ToDateTime(object? value)
    {
        switch (value) {
            case null:
                return null;
            case DateTime dt:
                return dt;
            case DateTimeOffset dto:
                return dto.UtcDateTime;
            case JsonElement { ValueKind: JsonValueKind.String } je:
                return ToDateTime(je.GetString());
            case JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }:
                return null;
            case string s when string.IsNullOrWhiteSpace(s):
                return null;
            case string s when DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedOffset):
                return parsedOffset.UtcDateTime;
            case string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed):
                return parsed;
            default:
                return TypeConversion.TryConvertTo<DateTime?>(value, out var converted) ? converted : null;
        }
    }

    private static IEnumerable<object?> Enumerate(object? value)
    {
        switch (value) {
            case null:
                yield break;
            case string:
                yield return value;
                yield break;
            case JsonElement { ValueKind: JsonValueKind.Array } arr:
                foreach (var el in arr.EnumerateArray())
                    yield return el;
                yield break;
            case IEnumerable<object?> objects:
                foreach (var item in objects)
                    yield return item;
                yield break;
            case System.Collections.IEnumerable enumerable:
                foreach (var item in enumerable)
                    yield return item;
                yield break;
            default:
                yield return value;
                break;
        }
    }
}
