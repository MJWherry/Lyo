using System.Diagnostics;
using System.Globalization;

namespace Lyo.DateAndTime;

/// <summary>A .NET Standard 2.0 compatible implementation of DateOnly matching the API of System.DateOnly introduced in .NET 6.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DateOnlyModel : IComparable<DateOnlyModel>, IEquatable<DateOnlyModel>
{
    private static readonly DateTime SEpoch = new(1, 1, 1);

    private readonly int _dayNumber;

    public int Year => ToDateTime().Year;

    public int Month => ToDateTime().Month;

    public int Day => ToDateTime().Day;

    public static DateOnlyModel Today => FromDateTime(DateTime.Today)!;

    public DateOnlyModel(int year, int month, int day) => _dayNumber = (new DateTime(year, month, day).Date - SEpoch).Days;

    private DateOnlyModel(int dayNumber) => _dayNumber = dayNumber;

    public int CompareTo(DateOnlyModel? other) => other == null ? 1 : _dayNumber.CompareTo(other._dayNumber);

    public bool Equals(DateOnlyModel? other) => other != null && _dayNumber == other._dayNumber;

    public static DateOnlyModel? FromDateTime(DateTime? dt)
    {
        if (!dt.HasValue)
            return null;

        var dayNumber = (dt.Value.Date - SEpoch).Days;
        return new(dayNumber);
    }

    public static DateOnlyModel Parse(string input, IFormatProvider? provider = null)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (!TryParse(input, provider, out var value))
            throw new FormatException($"Invalid date: '{input}'.");

        return value!;
    }

    public static bool TryParse(string? input, out DateOnlyModel? value) => TryParse(input, null, out value);

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

    public static DateOnlyModel ParseExact(string input, string format, IFormatProvider? provider = null)
    {
        if (!TryParseExact(input, format, provider, out var value))
            throw new FormatException($"Invalid date format: '{input}' for '{format}'.");

        return value!;
    }

    public static bool TryParseExact(string input, string format, IFormatProvider? provider, out DateOnlyModel? value)
    {
        if (DateTime.TryParseExact(input, format, provider, DateTimeStyles.None, out var dt)) {
            value = FromDateTime(dt);
            return true;
        }

        value = null;
        return false;
    }

    public DateTime ToDateTime() => SEpoch.AddDays(_dayNumber);

    public DateTime ToDateTime(TimeOnlyModel time) => ToDateTime().Add(time.ToTimeSpan());

    public DateOnlyModel AddDays(int days) => new(_dayNumber + days);

    public DateOnlyModel AddMonths(int months)
    {
        var dt = ToDateTime().AddMonths(months);
        return FromDateTime(dt)!;
    }

    public DateOnlyModel AddYears(int years)
    {
        var dt = ToDateTime().AddYears(years);
        return FromDateTime(dt)!;
    }

    public override string ToString() => ToDateTime().ToString("yyyy-MM-dd");

    public string ToString(string format) => ToDateTime().ToString(format);

    public string ToString(string format, IFormatProvider provider) => ToDateTime().ToString(format, provider);

    public override bool Equals(object? obj) => obj is DateOnlyModel other && Equals(other);

    public override int GetHashCode() => _dayNumber;

    public static bool operator ==(DateOnlyModel? left, DateOnlyModel? right) => Equals(left, right);

    public static bool operator !=(DateOnlyModel? left, DateOnlyModel? right) => !Equals(left, right);

    public static bool operator <(DateOnlyModel left, DateOnlyModel right) => left._dayNumber < right._dayNumber;

    public static bool operator >(DateOnlyModel left, DateOnlyModel right) => left._dayNumber > right._dayNumber;

    public static bool operator <=(DateOnlyModel left, DateOnlyModel right) => left._dayNumber <= right._dayNumber;

    public static bool operator >=(DateOnlyModel left, DateOnlyModel right) => left._dayNumber >= right._dayNumber;
}