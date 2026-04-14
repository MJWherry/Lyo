using System.Diagnostics;
using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.DateAndTime;

/// <summary>A .NET Standard 2.0 compatible implementation of TimeOnly matching the API of System.TimeOnly introduced in .NET 6.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TimeOnlyModel : IComparable<TimeOnlyModel>, IEquatable<TimeOnlyModel>
{
    public int Hour => new TimeSpan(Ticks).Hours;

    public int Minute => new TimeSpan(Ticks).Minutes;

    public int Second => new TimeSpan(Ticks).Seconds;

    public int Millisecond => new TimeSpan(Ticks).Milliseconds;

    public long Ticks { get; }

    public TimeOnlyModel(int hour, int minute)
        : this(new TimeSpan(hour, minute, 0).Ticks) { }

    public TimeOnlyModel(int hour, int minute, int second)
        : this(new TimeSpan(hour, minute, second).Ticks) { }

    public TimeOnlyModel(int hour, int minute, int second, int millisecond)
        : this(new TimeSpan(0, hour, minute, second, millisecond).Ticks) { }

    private TimeOnlyModel(long ticks)
    {
        if (ticks < 0 || ticks >= TimeSpan.TicksPerDay)
            throw new ArgumentOutOfRangeException(nameof(ticks));

        Ticks = ticks;
    }

    // Comparison
    public int CompareTo(TimeOnlyModel? other) => other == null ? 1 : Ticks.CompareTo(other.Ticks);

    // Equality
    public bool Equals(TimeOnlyModel? other) => other != null && Ticks == other.Ticks;

    public static TimeOnlyModel FromTimeSpan(TimeSpan ts)
    {
        if (ts.Ticks < 0 || ts.Ticks >= TimeSpan.TicksPerDay)
            throw new ArgumentOutOfRangeException(nameof(ts));

        return new(ts.Ticks);
    }

    public static TimeOnlyModel FromDateTime(DateTime dt)
    {
        var ts = dt.TimeOfDay;
        return new(ts.Ticks);
    }

    public static TimeOnlyModel Parse(string input, IFormatProvider? provider = null)
    {
        ArgumentHelpers.ThrowIfNull(input, nameof(input));
        if (!TryParse(input, provider, out var result))
            throw new FormatException($"Invalid time: '{input}'.");

        return result!;
    }

    public static bool TryParse(string? input, out TimeOnlyModel? value) => TryParse(input, null, out value);

    public static bool TryParse(string? input, IFormatProvider? provider, out TimeOnlyModel? value)
    {
        if (string.IsNullOrWhiteSpace(input)) {
            value = null;
            return false;
        }

        if (TimeSpan.TryParse(input, provider, out var ts)) {
            if (ts.Ticks >= 0 && ts.Ticks < TimeSpan.TicksPerDay) {
                value = FromTimeSpan(ts);
                return true;
            }
        }

        value = null;
        return false;
    }

    public static TimeOnlyModel ParseExact(string input, string format, IFormatProvider? provider = null)
    {
        if (!TryParseExact(input, format, provider, out var result))
            throw new FormatException($"Invalid time format: '{input}' for '{format}'.");

        return result!;
    }

    public static bool TryParseExact(string input, string format, IFormatProvider? provider, out TimeOnlyModel? value)
    {
        if (DateTime.TryParseExact(input, format, provider, DateTimeStyles.None, out var dt)) {
            value = FromDateTime(dt);
            return true;
        }

        if (TimeSpan.TryParseExact(input, format, provider, out var ts)) {
            if (ts.Ticks >= 0 && ts.Ticks < TimeSpan.TicksPerDay) {
                value = FromTimeSpan(ts);
                return true;
            }
        }

        value = null;
        return false;
    }

    public TimeSpan ToTimeSpan() => new(Ticks);

    public DateTime ToDateTime(DateOnlyModel date) => date.ToDateTime().AddTicks(Ticks);

    // Arithmetic
    public TimeOnlyModel Add(TimeSpan timespan)
    {
        var newTicks = (Ticks + timespan.Ticks) % TimeSpan.TicksPerDay;
        if (newTicks < 0)
            newTicks += TimeSpan.TicksPerDay;

        return new(newTicks);
    }

    public TimeOnlyModel AddHours(int hours) => Add(TimeSpan.FromHours(hours));

    public TimeOnlyModel AddMinutes(int minutes) => Add(TimeSpan.FromMinutes(minutes));

    public TimeOnlyModel AddSeconds(int seconds) => Add(TimeSpan.FromSeconds(seconds));

    public TimeOnlyModel AddMilliseconds(int ms) => Add(TimeSpan.FromMilliseconds(ms));

    // Formatting
    public override string ToString() => new TimeSpan(Ticks).ToString(@"hh\:mm\:ss");

    public string ToString(string format) => DateTime.Today.AddTicks(Ticks).ToString(format);

    public string ToString(string format, IFormatProvider provider) => DateTime.Today.AddTicks(Ticks).ToString(format, provider);

    public override bool Equals(object? obj) => obj is TimeOnlyModel other && Equals(other);

    public override int GetHashCode() => Ticks.GetHashCode();

    // Operators
    public static bool operator ==(TimeOnlyModel? left, TimeOnlyModel? right) => Equals(left, right);

    public static bool operator !=(TimeOnlyModel? left, TimeOnlyModel? right) => !Equals(left, right);

    public static bool operator <(TimeOnlyModel left, TimeOnlyModel right) => left.Ticks < right.Ticks;

    public static bool operator >(TimeOnlyModel left, TimeOnlyModel right) => left.Ticks > right.Ticks;

    public static bool operator <=(TimeOnlyModel left, TimeOnlyModel right) => left.Ticks <= right.Ticks;

    public static bool operator >=(TimeOnlyModel left, TimeOnlyModel right) => left.Ticks >= right.Ticks;
}