using System.Diagnostics;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class GeoTimeZone
{
    public string TimeZoneId { get; set; } = string.Empty;

    public string TimeZoneName { get; set; } = string.Empty;

    public TimeSpan UtcOffset { get; set; }

    public TimeSpan? DstOffset { get; set; }

    public bool IsDaylightSavingTime { get; set; }

    public DateTime? DstStart { get; set; }

    public DateTime? DstEnd { get; set; }

    public override string ToString()
        => $"GeoTimeZone: {TimeZoneId} ({TimeZoneName}), UTC{UtcOffset:hh\\:mm}, dst={IsDaylightSavingTime}";
}
