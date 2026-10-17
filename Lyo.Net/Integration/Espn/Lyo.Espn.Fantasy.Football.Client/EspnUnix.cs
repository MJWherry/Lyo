namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>Converts ESPN unix milliseconds without <c>DateTimeOffset.ToUnixTimeMilliseconds</c>, which netstandard2.0 does not have.</summary>
internal static class EspnUnix
{
    internal static DateTimeOffset ToOffset(long unixMilliseconds)
        => new(unixMilliseconds * TimeSpan.TicksPerMillisecond + 621355968000000000L, TimeSpan.Zero);

    internal static DateTimeOffset? ToOffset(long? unixMilliseconds)
        => unixMilliseconds == null ? null : ToOffset(unixMilliseconds.Value);
}
