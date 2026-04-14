namespace Lyo.Geolocation.Models;

public class BatchGeocodeResult
{
    public int TotalRequests { get; set; }

    public int SuccessfulResults { get; set; }

    public int FailedResults { get; set; }

    public IEnumerable<GeocodeResultItem> Results { get; set; }

    public TimeSpan ProcessingTime { get; set; }
}