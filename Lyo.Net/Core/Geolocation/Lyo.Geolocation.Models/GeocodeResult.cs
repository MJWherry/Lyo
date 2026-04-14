using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models;

public class GeocodeResult
{
    public Address Address { get; set; }

    public GeoCoordinate Coordinate { get; set; }

    public double ConfidenceScore { get; set; } // 0.0 - 1.0

    public GeocodingAccuracy Accuracy { get; set; }

    public BoundingBox ViewportBounds { get; set; }

    public string PlaceId { get; set; } // Provider-specific place identifier

    public GeocodeMatchType MatchType { get; set; }

    public IDictionary<string, string> Metadata { get; set; }
}