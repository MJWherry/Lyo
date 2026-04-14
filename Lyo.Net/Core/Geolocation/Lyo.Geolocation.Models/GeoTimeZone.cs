namespace Lyo.Geolocation.Models;

public class GeoTimeZone
{
    public string TimeZoneId { get; set; } // IANA time zone ID

    public string TimeZoneName { get; set; }

    public TimeSpan UtcOffset { get; set; }

    public TimeSpan? DstOffset { get; set; }

    public bool IsDaylightSavingTime { get; set; }

    public DateTime? DstStart { get; set; }

    public DateTime? DstEnd { get; set; }
}