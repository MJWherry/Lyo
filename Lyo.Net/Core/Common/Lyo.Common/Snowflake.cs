namespace Lyo.Common;

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