using Lyo.Exceptions;

namespace Lyo.Common.Identifiers;

/// <summary>
/// Parameters for extracting a millisecond timestamp from the high bits of a snowflake value. Different systems use different epoch origins and bit layouts (e.g. timestamp
/// field width).
/// </summary>
public readonly record struct SnowflakeLayout(int TimestampShiftBits, long EpochMillisecondsSinceUnixEpoch)
{
    public bool Equals(SnowflakeLayout other) => TimestampShiftBits == other.TimestampShiftBits && EpochMillisecondsSinceUnixEpoch == other.EpochMillisecondsSinceUnixEpoch;

    public override int GetHashCode()
    {
        unchecked {
            return (TimestampShiftBits * 397) ^ EpochMillisecondsSinceUnixEpoch.GetHashCode();
        }
    }
}

/// <summary>
/// A 64-bit snowflake identifier: a millisecond-resolution timestamp in the high bits and instance-specific data in the low bits. The exact layout is defined by
/// <see cref="SnowflakeLayout" />.
/// </summary>
public readonly record struct Snowflake(ulong Value) : IComparable<Snowflake>
{
    public int CompareTo(Snowflake other) => Value.CompareTo(other.Value);

    public bool Equals(Snowflake other) => Value == other.Value;

    /// <summary>Interprets a signed snowflake as stored by many APIs and databases (same bit pattern as <see cref="ulong" />).</summary>
    public static Snowflake FromInt64(long signed) => new(unchecked((ulong)signed));

    public long ToInt64() => unchecked((long)Value);

    /// <summary>Milliseconds since the Unix epoch derived from the timestamp field using <paramref name="layout" />.</summary>
    public long GetUnixMillisecondsSinceUnixEpoch(SnowflakeLayout layout)
    {
        var t = (long)(Value >> layout.TimestampShiftBits);
        return t + layout.EpochMillisecondsSinceUnixEpoch;
    }

    public DateTimeOffset GetTimestampUtc(SnowflakeLayout layout) => DateTimeOffset.FromUnixTimeMilliseconds(GetUnixMillisecondsSinceUnixEpoch(layout));

    public DateTime GetUtcDateTime(SnowflakeLayout layout) => GetTimestampUtc(layout).UtcDateTime;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Thread-safe generator that produces monotonically increasing 64-bit <see cref="Snowflake" /> identifiers. The bit layout is: 1 unused sign bit, 41-bit millisecond
/// timestamp (relative to <see cref="DefaultEpochMs" />), 10-bit machine ID, 12-bit sequence counter — yielding up to 4 096 unique IDs per millisecond per machine.
/// </summary>
public sealed class SnowflakeGenerator
{
    private const int SequenceBits = 12;
    private const int MachineIdBits = 10;

    // TimestampShiftBits used when building IDs and when constructing the Layout for parsing.
    private const int TimestampShift = SequenceBits + MachineIdBits; // 22

    private const int MaxMachineId = (1 << MachineIdBits) - 1; // 1 023
    private const int MaxSequence = (1 << SequenceBits) - 1; // 4 095

    /// <summary>Milliseconds since the Unix epoch for the default snowflake epoch: 2020-01-01T00:00:00Z.</summary>
    public static readonly long DefaultEpochMs = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

    private readonly long _epochMs;
    private readonly object _lock = new();

    private readonly long _machineIdShifted;
    private long _lastTimestampMs = -1;
    private int _sequence;

    /// <summary>A shared default generator instance (machine ID 0, <see cref="DefaultEpochMs" />).</summary>
    public static SnowflakeGenerator Shared { get; } = new();

    /// <summary>The machine ID this generator was constructed with (0–1023).</summary>
    public int MachineId { get; }

    /// <summary>
    /// The <see cref="SnowflakeLayout" /> that matches this generator's bit layout. Pass it to <see cref="Snowflake.GetTimestampUtc" /> or
    /// <see cref="Snowflake.GetUnixMillisecondsSinceUnixEpoch" /> to round-trip the embedded timestamp.
    /// </summary>
    public SnowflakeLayout Layout => new(TimestampShift, _epochMs);

    /// <summary>Creates a generator with machine ID 0 and the <see cref="DefaultEpochMs">default epoch</see>.</summary>
    public SnowflakeGenerator()
        : this(0) { }

    /// <summary>Creates a generator for the specified <paramref name="machineId" /> (0–1023) using the <see cref="DefaultEpochMs">default epoch</see>.</summary>
    public SnowflakeGenerator(int machineId)
        : this(machineId, DefaultEpochMs) { }

    /// <summary>Creates a generator for the specified <paramref name="machineId" /> (0–1023) and a custom <paramref name="epochMs" /> (milliseconds since the Unix epoch).</summary>
    public SnowflakeGenerator(int machineId, long epochMs)
    {
        ArgumentHelpers.ThrowIf(machineId < 0 || machineId > MaxMachineId, $"Machine ID must be between 0 and {MaxMachineId}.", nameof(machineId));
        MachineId = machineId;
        _machineIdShifted = (long)machineId << SequenceBits;
        _epochMs = epochMs;
    }

    /// <summary>Generates the next snowflake. Thread-safe; spins for up to 1 ms if the sequence counter is exhausted within the current millisecond.</summary>
    public Snowflake Next()
    {
        lock (_lock)
            return NextLocked();
    }

    /// <summary>Generates <paramref name="count" /> snowflakes in ascending order. Thread-safe.</summary>
    public Snowflake[] NextBulk(int count)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(count);
        var result = new Snowflake[count];
        lock (_lock) {
            for (var i = 0; i < count; i++)
                result[i] = NextLocked();
        }

        return result;
    }

    private Snowflake NextLocked()
    {
        var now = CurrentMs();
        if (now == _lastTimestampMs) {
            _sequence = (_sequence + 1) & MaxSequence;
            if (_sequence == 0)
                now = WaitNextMs(_lastTimestampMs);
        }
        else
            _sequence = 0;

        _lastTimestampMs = now;
        var value = ((ulong)(now - _epochMs) << TimestampShift) | (ulong)_machineIdShifted | (uint)_sequence;
        return new(value);
    }

    private static long CurrentMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static long WaitNextMs(long lastMs)
    {
        long now;
        do
            now = CurrentMs();
        while (now <= lastMs);

        return now;
    }
}