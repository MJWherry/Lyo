namespace Lyo.Geolocation.Models;

public class GeocodeOptions
{
    public string Language { get; set; }

    public string Region { get; set; } // Bias results to a region

    public BoundingBox Bounds { get; set; } // Restrict to bounds

    public int? MaxResults { get; set; }

    public IEnumerable<string> ComponentRestrictions { get; set; }
}