using System.Diagnostics;
using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.DateAndTime;

/// <summary>
/// .NET Standard 2.0–compatible calendar date without a time-of-day component, modeled after the BCL <c>System.DateOnly</c> type introduced in .NET 6.
/// </summary>
/// <remarks>
/// Values are stored as a day offset from 0001-01-01, mirroring the BCL behavior closely enough for scheduling and persistence scenarios where
/// <see cref="DateTime" /> would introduce unwanted time zones. On .NET 6+, prefer the built-in <c>System.DateOnly</c> type when you can target it directly.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DateOnlyModel : IComparable<DateOnlyModel>, IEquatable<DateOnlyModel>
{
    private static readonly DateTime SEpoch = new(1, 1, 1);

    private readonly int _dayNumber;

    /// <summary>Calendar year component for this date.</summary>
    public int Year => ToDateTime().Year;

    /// <summary>Calendar month component (1–12).</summary>
    public int Month => ToDateTime().Month;

    /// <summary>Calendar day component (1–31).</summary>
    public int Day => ToDateTime().Day;

    /// <summary>Gets today’s date in the local time zone.</summary>
    public static DateOnlyModel Today => FromDateTime(DateTime.Today)!;

    /// <summary>Constructs a date equivalent to <c>new DateTime(year, month, day)</c>.</summary>
    /// <param name="year">Gregorian year.</param>
    /// <param name="month">Gregorian month.</param>
    /// <param name="day">Gregorian day.</param>
    public DateOnlyModel(int year, int month, int day) => _dayNumber = (new DateTime(year, month, day).Date - SEpoch).Days;

    private DateOnlyModel(int dayNumber) => _dayNumber = dayNumber;

    /// <inheritdoc />
    public int CompareTo(DateOnlyModel? other) => other == null ? 1 : _dayNumber.CompareTo(other._dayNumber);

    /// <inheritdoc />
    public bool Equals(DateOnlyModel? other) => other != null && _dayNumber == other._dayNumber;

    /// <summary>Converts the date portion of a <see cref="DateTime" /> (or <see langword="null" />).</summary>
    /// <param name="dt">Source instant; only <see cref="DateTime.Date" /> is retained.</param>
    /// <returns><see langword="null" /> when <paramref name="dt" /> is <see langword="null" />.</returns>
    public static DateOnlyModel? FromDateTime(DateTime? dt)
    {
        if (!dt.HasValue)
            return null;

        var dayNumber = (dt.Value.Date - SEpoch).Days;
        return new(dayNumber);
    }

    /// <summary>Parses a culture-aware date string, throwing on failure.</summary>
    /// <param name="input">Text accepted by <see cref="DateTime.Parse(string, IFormatProvider?, System.Globalization.DateTimeStyles)" />.</param>
    /// <param name="provider">Optional culture-specific parsing rules.</param>
    /// <returns>The parsed date.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">The text cannot be parsed as a date.</exception>
    public static DateOnlyModel Parse(string input, IFormatProvider? provider = null)
    {
        ArgumentHelpers.ThrowIfNull(input);
        if (!TryParse(input, provider, out var value))
            throw new FormatException($"Invalid date: '{input}'.");

        return value!;
    }

    /// <summary>Attempts to parse using the invariant or supplied culture.</summary>
    public static bool TryParse(string? input, out DateOnlyModel? value) => TryParse(input, null, out value);

    /// <summary>Culture-aware parsing that returns <see langword="false" /> instead of throwing.</summary>
    /// <param name="input">Candidate text.</param>
    /// <param name="provider">Optional culture-specific parsing rules.</param>
    /// <param name="value">Parsed value when the method returns <see langword="true" />.</param>
    public static bool TryParse(string? input, IFormatProvider? provider, out DateOnlyModel? value)
    {
        if (string.IsNullOrWhiteSpace(input)) {
            value = null;
            return false;
        }

        if (DateTime.TryParse(input, provider, DateTimeStyles.None, out var dt)) {
            value = FromDateTime(dt);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Parses using an exact format string.</summary>
    public static DateOnlyModel ParseExact(string input, string format, IFormatProvider? provider = null)
    {
        if (!TryParseExact(input, format, provider, out var value))
            throw new FormatException($"Invalid date format: '{input}' for '{format}'.");

        return value!;
    }

    /// <summary>Exact-format parsing that returns <see langword="false" /> instead of throwing.</summary>
    public static bool TryParseExact(string input, string format, IFormatProvider? provider, out DateOnlyModel? value)
    {
        if (DateTime.TryParseExact(input, format, provider, DateTimeStyles.None, out var dt)) {
            value = FromDateTime(dt);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Maps this date to midnight on the same calendar day.</summary>
    public DateTime ToDateTime() => SEpoch.AddDays(_dayNumber);

    /// <summary>Combines the calendar date with a time-of-day.</summary>
    public DateTime ToDateTime(TimeOnlyModel time) => ToDateTime().Add(time.ToTimeSpan());

    /// <summary>Adds signed days with calendar-aware wrapping (via <see cref="DateTime" />).</summary>
    public DateOnlyModel AddDays(int days) => new(_dayNumber + days);

    /// <summary>Adds months using Gregorian calendar rules.</summary>
    public DateOnlyModel AddMonths(int months)
    {
        var dt = ToDateTime().AddMonths(months);
        return FromDateTime(dt)!;
    }

    /// <summary>Adds years using Gregorian calendar rules.</summary>
    public DateOnlyModel AddYears(int years)
    {
        var dt = ToDateTime().AddYears(years);
        return FromDateTime(dt)!;
    }

    /// <inheritdoc />
    public override string ToString() => ToDateTime().ToString("yyyy-MM-dd");

    /// <summary>Formats using the same rules as <see cref="DateTime.ToString(string)" /> on the underlying midnight <see cref="DateTime" />.</summary>
    public string ToString(string format) => ToDateTime().ToString(format);

    /// <summary>Formats using the same rules as <see cref="DateTime.ToString(string, IFormatProvider)" />.</summary>
    public string ToString(string format, IFormatProvider provider) => ToDateTime().ToString(format, provider);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DateOnlyModel other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _dayNumber;

    /// <summary>Equality comparison.</summary>
    public static bool operator ==(DateOnlyModel? left, DateOnlyModel? right) => Equals(left, right);

    /// <summary>Inequality comparison.</summary>
    public static bool operator !=(DateOnlyModel? left, DateOnlyModel? right) => !Equals(left, right);

    /// <summary>Lexicographic ordering by underlying day number.</summary>
    public static bool operator <(DateOnlyModel left, DateOnlyModel right) => left._dayNumber < right._dayNumber;

    /// <summary>Determines whether <paramref name="left" /> occurs after <paramref name="right" />.</summary>
    public static bool operator >(DateOnlyModel left, DateOnlyModel right) => left._dayNumber > right._dayNumber;

    /// <summary>Determines whether <paramref name="left" /> occurs on or before <paramref name="right" />.</summary>
    public static bool operator <=(DateOnlyModel left, DateOnlyModel right) => left._dayNumber <= right._dayNumber;

    /// <summary>Determines whether <paramref name="left" /> occurs on or after <paramref name="right" />.</summary>
    public static bool operator >=(DateOnlyModel left, DateOnlyModel right) => left._dayNumber >= right._dayNumber;
}
