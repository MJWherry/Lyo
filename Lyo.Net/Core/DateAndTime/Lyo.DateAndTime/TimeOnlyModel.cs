using System.Diagnostics;
using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.DateAndTime;

/// <summary>
/// .NET Standard 2.0–compatible time-of-day value (no calendar), modeled after the BCL <c>System.TimeOnly</c> type introduced in .NET 6.
/// </summary>
/// <remarks>
/// Values are stored as ticks within a single 24-hour day <c>[0, TimeSpan.TicksPerDay)</c>. Arithmetic wraps modulo one day, mirroring the BCL semantics closely enough
/// for scheduling alongside <see cref="DateOnlyModel" />.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TimeOnlyModel : IComparable<TimeOnlyModel>, IEquatable<TimeOnlyModel>
{
    /// <summary>Hour-of-day component (0–23) derived from <see cref="Ticks" />.</summary>
    public int Hour => new TimeSpan(Ticks).Hours;

    /// <summary>Minute component (0–59).</summary>
    public int Minute => new TimeSpan(Ticks).Minutes;

    /// <summary>Second component (0–59).</summary>
    public int Second => new TimeSpan(Ticks).Seconds;

    /// <summary>Millisecond component (0–999).</summary>
    public int Millisecond => new TimeSpan(Ticks).Milliseconds;

    /// <summary>Ticks since midnight, strictly less than <see cref="TimeSpan.TicksPerDay" />.</summary>
    public long Ticks { get; }

    /// <summary>Constructs a time from hour and minute (seconds and milliseconds zero).</summary>
    public TimeOnlyModel(int hour, int minute)
        : this(new TimeSpan(hour, minute, 0).Ticks) { }

    /// <summary>Constructs a time from hour, minute, and second (milliseconds zero).</summary>
    public TimeOnlyModel(int hour, int minute, int second)
        : this(new TimeSpan(hour, minute, second).Ticks) { }

    /// <summary>Constructs a time including fractional seconds.</summary>
    public TimeOnlyModel(int hour, int minute, int second, int millisecond)
        : this(new TimeSpan(0, hour, minute, second, millisecond).Ticks) { }

    /// <summary>Internal constructor enforcing the tick range invariant.</summary>
    private TimeOnlyModel(long ticks)
    {
        ArgumentHelpers.ThrowIfNotInRange(ticks, 0L, TimeSpan.TicksPerDay - 1);
        Ticks = ticks;
    }

    /// <inheritdoc />
    public int CompareTo(TimeOnlyModel? other) => other == null ? 1 : Ticks.CompareTo(other.Ticks);

    /// <inheritdoc />
    public bool Equals(TimeOnlyModel? other) => other != null && Ticks == other.Ticks;

    /// <summary>Creates a model from a same-day <see cref="TimeSpan" />.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The span is outside one day.</exception>
    public static TimeOnlyModel FromTimeSpan(TimeSpan ts)
    {
        ArgumentHelpers.ThrowIfNotInRange(ts.Ticks, 0L, TimeSpan.TicksPerDay - 1, nameof(ts));
        return new(ts.Ticks);
    }

    /// <summary>Extracts the time-of-day portion of <paramref name="dt" />.</summary>
    public static TimeOnlyModel FromDateTime(DateTime dt)
    {
        var ts = dt.TimeOfDay;
        return new(ts.Ticks);
    }

    /// <summary>Parses a time string, throwing on failure.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">The text cannot be parsed as a time-of-day.</exception>
    public static TimeOnlyModel Parse(string input, IFormatProvider? provider = null)
    {
        ArgumentHelpers.ThrowIfNull(input);
        if (!TryParse(input, provider, out var result))
            throw new FormatException($"Invalid time: '{input}'.");

        return result!;
    }

    /// <summary>Attempts to parse using optional culture rules.</summary>
    public static bool TryParse(string? input, out TimeOnlyModel? value) => TryParse(input, null, out value);

    /// <summary>Culture-aware parsing that returns <see langword="false" /> instead of throwing.</summary>
    /// <param name="input">Candidate text.</param>
    /// <param name="provider">Optional culture-specific parsing rules.</param>
    /// <param name="value">Parsed value when the method returns <see langword="true" />.</param>
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

    /// <summary>Parses using an exact format string.</summary>
    public static TimeOnlyModel ParseExact(string input, string format, IFormatProvider? provider = null)
    {
        if (!TryParseExact(input, format, provider, out var result))
            throw new FormatException($"Invalid time format: '{input}' for '{format}'.");

        return result!;
    }

    /// <summary>Exact-format parsing that returns <see langword="false" /> instead of throwing.</summary>
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

    /// <summary>Converts to a <see cref="TimeSpan" /> representing elapsed time since midnight.</summary>
    public TimeSpan ToTimeSpan() => new(Ticks);

    /// <summary>Combines a calendar date with this time-of-day in an unspecified <see cref="DateTimeKind" />.</summary>
    public DateTime ToDateTime(DateOnlyModel date) => date.ToDateTime().AddTicks(Ticks);

    /// <summary>Adds a duration with wrap-around within the 24-hour clock.</summary>
    public TimeOnlyModel Add(TimeSpan timespan)
    {
        var newTicks = (Ticks + timespan.Ticks) % TimeSpan.TicksPerDay;
        if (newTicks < 0)
            newTicks += TimeSpan.TicksPerDay;

        return new(newTicks);
    }

    /// <summary>Adds whole hours with wrap-around.</summary>
    public TimeOnlyModel AddHours(int hours) => Add(TimeSpan.FromHours(hours));

    /// <summary>Adds whole minutes with wrap-around.</summary>
    public TimeOnlyModel AddMinutes(int minutes) => Add(TimeSpan.FromMinutes(minutes));

    /// <summary>Adds whole seconds with wrap-around.</summary>
    public TimeOnlyModel AddSeconds(int seconds) => Add(TimeSpan.FromSeconds(seconds));

    /// <summary>Adds whole milliseconds with wrap-around.</summary>
    public TimeOnlyModel AddMilliseconds(int ms) => Add(TimeSpan.FromMilliseconds(ms));

    /// <inheritdoc />
    public override string ToString() => new TimeSpan(Ticks).ToString(@"hh\:mm\:ss");

    /// <summary>Formats using a <see cref="DateTime" /> surrogate anchored at today.</summary>
    public string ToString(string format) => DateTime.Today.AddTicks(Ticks).ToString(format);

    /// <inheritdoc cref="ToString(string)" />
    public string ToString(string format, IFormatProvider provider) => DateTime.Today.AddTicks(Ticks).ToString(format, provider);

    public override bool Equals(object? obj) => obj is TimeOnlyModel other && Equals(other);

    public override int GetHashCode() => Ticks.GetHashCode();

    /// <summary>Equality comparison.</summary>
    public static bool operator ==(TimeOnlyModel? left, TimeOnlyModel? right) => Equals(left, right);

    /// <summary>Inequality comparison.</summary>
    public static bool operator !=(TimeOnlyModel? left, TimeOnlyModel? right) => !Equals(left, right);

    /// <summary>Compares underlying tick counts.</summary>
    public static bool operator <(TimeOnlyModel left, TimeOnlyModel right) => left.Ticks < right.Ticks;

    /// <summary>Compares underlying tick counts.</summary>
    public static bool operator >(TimeOnlyModel left, TimeOnlyModel right) => left.Ticks > right.Ticks;

    /// <summary>Compares underlying tick counts.</summary>
    public static bool operator <=(TimeOnlyModel left, TimeOnlyModel right) => left.Ticks <= right.Ticks;

    /// <summary>Compares underlying tick counts.</summary>
    public static bool operator >=(TimeOnlyModel left, TimeOnlyModel right) => left.Ticks >= right.Ticks;
}