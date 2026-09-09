using System.Diagnostics;
using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.DateAndTime;

/// <summary>Calendar date with no time-of-day, usable on .NET Standard 2.0, modeled after the BCL <c>System.DateOnly</c> type added in .NET 6.</summary>
/// <remarks>
/// Stored as a day offset from 0001-01-01, close enough to BCL behavior for scheduling and persistence where <see cref="DateTime" /> would bring in unwanted time zones.
/// On .NET 6+, prefer built-in <c>System.DateOnly</c> when you can target it directly.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DateOnlyModel : IComparable<DateOnlyModel>, IEquatable<DateOnlyModel>
{
    private static readonly DateTime SEpoch = new(1, 1, 1);

    private readonly int _dayNumber;

    /// <summary>Year of this calendar date.</summary>
    public int Year => ToDateTime().Year;

    /// <summary>Month of this calendar date (1–12).</summary>
    public int Month => ToDateTime().Month;

    /// <summary>Day of this calendar date (1–31).</summary>
    public int Day => ToDateTime().Day;

    /// <summary>Today's date in the local time zone.</summary>
    public static DateOnlyModel Today => FromDateTime(DateTime.Today)!;

    /// <summary>Builds a date matching <c>new DateTime(year, month, day)</c>.</summary>
    /// <param name="year">Gregorian year.</param>
    /// <param name="month">Gregorian month.</param>
    /// <param name="day">Gregorian day.</param>
    public DateOnlyModel(int year, int month, int day) => _dayNumber = (new DateTime(year, month, day).Date - SEpoch).Days;

    private DateOnlyModel(int dayNumber) => _dayNumber = dayNumber;

    /// <inheritdoc />
    public int CompareTo(DateOnlyModel? other) => other == null ? 1 : _dayNumber.CompareTo(other._dayNumber);

    /// <inheritdoc />
    public bool Equals(DateOnlyModel? other) => other != null && _dayNumber == other._dayNumber;

    /// <summary>Takes the calendar-date part of a <see cref="DateTime" /> (or <see langword="null" />).</summary>
    /// <param name="dt">Source instant; only <see cref="DateTime.Date" /> is kept.</param>
    /// <returns><see langword="null" /> when <paramref name="dt" /> is <see langword="null" />.</returns>
    public static DateOnlyModel? FromDateTime(DateTime? dt)
    {
        if (!dt.HasValue)
            return null;

        var dayNumber = (dt.Value.Date - SEpoch).Days;
        return new(dayNumber);
    }

    /// <summary>Parses a culture-aware date string and throws if it fails.</summary>
    /// <param name="input">Text accepted by <see cref="DateTime.Parse(string, IFormatProvider?, System.Globalization.DateTimeStyles)" />.</param>
    /// <param name="provider">Optional culture-specific parse rules.</param>
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

    /// <summary>Tries to parse with the invariant culture, or the one supplied.</summary>
    public static bool TryParse(string? input, out DateOnlyModel? value) => TryParse(input, null, out value);

    /// <summary>Culture-aware parse that returns <see langword="false" /> instead of throwing.</summary>
    /// <param name="input">Text to try.</param>
    /// <param name="provider">Optional culture-specific parse rules.</param>
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

    /// <summary>Parses against an exact format string.</summary>
    public static DateOnlyModel ParseExact(string input, string format, IFormatProvider? provider = null)
    {
        if (!TryParseExact(input, format, provider, out var value))
            throw new FormatException($"Invalid date format: '{input}' for '{format}'.");

        return value!;
    }

    /// <summary>Exact-format parse that returns <see langword="false" /> instead of throwing.</summary>
    public static bool TryParseExact(string input, string format, IFormatProvider? provider, out DateOnlyModel? value)
    {
        if (DateTime.TryParseExact(input, format, provider, DateTimeStyles.None, out var dt)) {
            value = FromDateTime(dt);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Maps this date to midnight on that calendar day.</summary>
    public DateTime ToDateTime() => SEpoch.AddDays(_dayNumber);

    /// <summary>Joins this calendar date with a time-of-day.</summary>
    public DateTime ToDateTime(TimeOnlyModel time) => ToDateTime().Add(time.ToTimeSpan());

    /// <summary>Adds a signed day count with calendar-aware wrap (via <see cref="DateTime" />).</summary>
    public DateOnlyModel AddDays(int days) => new(_dayNumber + days);

    /// <summary>Adds months under Gregorian calendar rules.</summary>
    public DateOnlyModel AddMonths(int months)
    {
        var dt = ToDateTime().AddMonths(months);
        return FromDateTime(dt)!;
    }

    /// <summary>Adds years under Gregorian calendar rules.</summary>
    public DateOnlyModel AddYears(int years)
    {
        var dt = ToDateTime().AddYears(years);
        return FromDateTime(dt)!;
    }

    /// <inheritdoc />
    public override string ToString() => ToDateTime().ToString("yyyy-MM-dd");

    /// <summary>Formats with the same rules as <see cref="DateTime.ToString(string)" /> on the underlying midnight <see cref="DateTime" />.</summary>
    public string ToString(string format) => ToDateTime().ToString(format);

    /// <summary>Formats with the same rules as <see cref="DateTime.ToString(string, IFormatProvider)" />.</summary>
    public string ToString(string format, IFormatProvider provider) => ToDateTime().ToString(format, provider);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DateOnlyModel other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _dayNumber;

    /// <summary>True when both dates are equal.</summary>
    public static bool operator ==(DateOnlyModel? left, DateOnlyModel? right) => Equals(left, right);

    /// <summary>True when the dates differ.</summary>
    public static bool operator !=(DateOnlyModel? left, DateOnlyModel? right) => !Equals(left, right);

    /// <summary>Orders by the underlying day number.</summary>
    public static bool operator <(DateOnlyModel left, DateOnlyModel right) => left._dayNumber < right._dayNumber;

    /// <summary>True when <paramref name="left" /> is after <paramref name="right" />.</summary>
    public static bool operator >(DateOnlyModel left, DateOnlyModel right) => left._dayNumber > right._dayNumber;

    /// <summary>True when <paramref name="left" /> is on or before <paramref name="right" />.</summary>
    public static bool operator <=(DateOnlyModel left, DateOnlyModel right) => left._dayNumber <= right._dayNumber;

    /// <summary>True when <paramref name="left" /> is on or after <paramref name="right" />.</summary>
    public static bool operator >=(DateOnlyModel left, DateOnlyModel right) => left._dayNumber >= right._dayNumber;
}