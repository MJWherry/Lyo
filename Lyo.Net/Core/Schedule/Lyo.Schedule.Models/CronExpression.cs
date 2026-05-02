namespace Lyo.Schedule.Models;

/// <summary>
/// Parses cron expressions and calculates the next occurrence time.
/// Supports standard 5-field and extended 6-field (with seconds) formats.
/// Field syntax: values, ranges (n-m), steps (*/n or n-m/n), comma-separated lists,
/// named months (JAN-DEC) and days (SUN-SAT), and <c>?</c> as an alias for <c>*</c>.
/// When both day-of-month and day-of-week are restricted, OR logic is applied (Vixie cron behaviour).
/// </summary>
public sealed class CronExpression
{
    private readonly bool _domRestricted;
    private readonly bool _dowRestricted;
    private readonly SortedSet<int> _seconds;
    private readonly SortedSet<int> _minutes;
    private readonly SortedSet<int> _hours;
    private readonly SortedSet<int> _daysOfMonth;
    private readonly SortedSet<int> _months;
    private readonly SortedSet<int> _daysOfWeek; // 0=Sunday … 6=Saturday

    // Index matches the numeric value (index 0 is unused for months).
    private static readonly string[] MonthNames = [string.Empty, "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"];

    private static readonly string[] DayOfWeekNames = ["SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"];

    private CronExpression(
        bool domRestricted, bool dowRestricted,
        SortedSet<int> seconds, SortedSet<int> minutes, SortedSet<int> hours,
        SortedSet<int> daysOfMonth, SortedSet<int> months, SortedSet<int> daysOfWeek)
    {
        _domRestricted = domRestricted;
        _dowRestricted = dowRestricted;
        _seconds = seconds;
        _minutes = minutes;
        _hours = hours;
        _daysOfMonth = daysOfMonth;
        _months = months;
        _daysOfWeek = daysOfWeek;
    }

    /// <summary>Parses a cron expression.</summary>
    /// <exception cref="FormatException">The expression is syntactically invalid.</exception>
    public static CronExpression Parse(string expression, CronFormat format = CronFormat.Standard)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new FormatException("Cron expression cannot be null or empty.");

        var parts = expression.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        bool hasSeconds = format == CronFormat.IncludeSeconds;
        int expectedParts = hasSeconds ? 6 : 5;

        if (parts.Length != expectedParts)
            throw new FormatException(
                $"Expected {expectedParts} fields but found {parts.Length} in cron expression '{expression}'.");

        var i = 0;
        var seconds = hasSeconds ? ParseField(parts[i++], 0, 59, null) : [0];
        var minutes = ParseField(parts[i++], 0, 59, null);
        var hours = ParseField(parts[i++], 0, 23, null);
        var domRestricted = !IsWildcard(parts[i]);
        var doms = ParseField(parts[i++], 1, 31, null);
        var months = ParseField(parts[i++], 1, 12, MonthNames);
        var dowRestricted = !IsWildcard(parts[i]);
        var rawDow = ParseField(parts[i], 0, 7, DayOfWeekNames);
        // Normalize alternate Sunday value (7) to 0.
        var dows = new SortedSet<int>(rawDow.Select(d => d % 7));

        return new CronExpression(domRestricted, dowRestricted, seconds, minutes, hours, doms, months, dows);
    }

    /// <summary>
    /// Returns the next occurrence strictly after <paramref name="from"/>, evaluated in
    /// <paramref name="zone"/>. Returns <c>null</c> if no occurrence is found within four years.
    /// </summary>
    public DateTimeOffset? GetNextOccurrence(DateTimeOffset from, TimeZoneInfo zone)
    {
        var localFrom = TimeZoneInfo.ConvertTime(from, zone);

        // Advance by one second and strip sub-second precision.
        var dt = new DateTime(
            localFrom.Year, localFrom.Month, localFrom.Day,
            localFrom.Hour, localFrom.Minute, localFrom.Second,
            DateTimeKind.Unspecified).AddSeconds(1);

        var limit = dt.AddYears(4);

        while (dt <= limit) {
            if (!_months.Contains(dt.Month)) {
                dt = AdvanceToNextMonth(dt);
                continue;
            }

            if (!IsDayMatch(dt)) {
                dt = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Unspecified).AddDays(1);
                continue;
            }

            if (!_hours.Contains(dt.Hour)) {
                var nextHour = FirstGreaterThan(_hours, dt.Hour);
                dt = nextHour.HasValue
                    ? new(dt.Year, dt.Month, dt.Day, nextHour.Value, 0, 0, DateTimeKind.Unspecified)
                    : new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Unspecified).AddDays(1);
                continue;
            }

            if (!_minutes.Contains(dt.Minute)) {
                var nextMin = FirstGreaterThan(_minutes, dt.Minute);
                dt = nextMin.HasValue
                    ? new(dt.Year, dt.Month, dt.Day, dt.Hour, nextMin.Value, 0, DateTimeKind.Unspecified)
                    : new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Unspecified).AddHours(1);
                continue;
            }

            if (!_seconds.Contains(dt.Second)) {
                var nextSec = FirstGreaterThan(_seconds, dt.Second);
                dt = nextSec.HasValue
                    ? new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, nextSec.Value, DateTimeKind.Unspecified)
                    : new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Unspecified).AddMinutes(1);
                continue;
            }

            // All fields match. Convert to UTC, handling DST gaps gracefully.
            try {
                var utc = TimeZoneInfo.ConvertTimeToUtc(dt, zone);
                return new DateTimeOffset(utc, TimeSpan.Zero);
            }
            catch (ArgumentException) {
                // Local time falls inside a DST spring-forward gap; advance past it.
                dt = dt.AddMinutes(1);
            }
        }

        return null;
    }

    private bool IsDayMatch(DateTime dt)
    {
        // Vixie cron: OR when both fields are restricted; otherwise the restricted field wins.
        if (_domRestricted && _dowRestricted)
            return _daysOfMonth.Contains(dt.Day) || _daysOfWeek.Contains((int)dt.DayOfWeek);
        if (_domRestricted)
            return _daysOfMonth.Contains(dt.Day);
        if (_dowRestricted)
            return _daysOfWeek.Contains((int)dt.DayOfWeek);
        return true;
    }

    private DateTime AdvanceToNextMonth(DateTime dt)
    {
        var next = FirstGreaterThan(_months, dt.Month);
        return next.HasValue
            ? new(dt.Year, next.Value, 1, 0, 0, 0, DateTimeKind.Unspecified)
            : new DateTime(dt.Year + 1, _months.Min, 1, 0, 0, 0, DateTimeKind.Unspecified);
    }

    private static int? FirstGreaterThan(SortedSet<int> set, int value)
    {
        foreach (var v in set)
            if (v > value) return v;
        return null;
    }

    private static bool IsWildcard(string field) => field == "*" || field == "?";

    private static SortedSet<int> ParseField(string field, int min, int max, string[]? names)
    {
        var result = new SortedSet<int>();
        foreach (var segment in field.Split(',')) {
            var seg = segment.Trim();
            if (seg.Length == 0)
                throw new FormatException($"Empty segment in cron field '{field}'.");
            ParseSegment(seg, min, max, names, result);
        }

        return result.Count == 0 
            ? throw new FormatException($"No valid values in cron field '{field}'.") 
            : result;
    }

    private static void ParseSegment(string seg, int min, int max, string[]? names, SortedSet<int> result)
    {
        var step = 1;
        var main = seg;
        var slashIndex = seg.IndexOf('/');
        if (slashIndex >= 0) {
            main = seg[..slashIndex];
            if (!int.TryParse(seg[(slashIndex + 1)..], out step) || step <= 0)
                throw new FormatException($"Invalid step value in cron segment '{seg}'.");
        }

        int rangeFrom, rangeTo;
        if (main is "*" or "?") {
            rangeFrom = min;
            rangeTo = max;
        }
        else {
            var dashIndex = main.IndexOf('-', 1); // skip index 0 to avoid treating a leading '-' as a dash
            if (dashIndex > 0) {
                rangeFrom = ParseSingleValue(main.Substring(0, dashIndex), min, max, names);
                rangeTo = ParseSingleValue(main.Substring(dashIndex + 1), min, max, names);
                if (rangeFrom > rangeTo)
                    throw new FormatException($"Range start exceeds range end in cron segment '{seg}'.");
            }
            else {
                rangeFrom = ParseSingleValue(main, min, max, names);
                rangeTo = slashIndex >= 0 ? max : rangeFrom;
            }
        }

        for (var v = rangeFrom; v <= rangeTo; v += step)
            result.Add(v);
    }

    private static int ParseSingleValue(string s, int min, int max, string[]? names)
    {
        if (names != null) {
            for (var i = 0; i < names.Length; i++) {
                if (names[i].Length > 0 && string.Equals(s, names[i], StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }

        if (!int.TryParse(s, out var val))
            throw new FormatException($"Cannot parse '{s}' as a cron field value.");
        if (val < min || val > max)
            throw new FormatException($"Value {val} is outside the allowed range [{min}, {max}].");
        return val;
    }
}
