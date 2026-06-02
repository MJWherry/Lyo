using System.Diagnostics;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class GeocodeResult
{
    public Address Address { get; set; } = null!;

    public GeoCoordinate Coordinate { get; set; } = null!;

    public double ConfidenceScore { get; set; }

    public GeocodingAccuracy Accuracy { get; set; }

    public BoundingBox ViewportBounds { get; set; } = null!;

    public string PlaceId { get; set; } = string.Empty;

    public GeocodeMatchType MatchType { get; set; }

    public IDictionary<string, string>? Metadata { get; set; }

    public override string ToString() => $"GeocodeResult: {Address}, conf={ConfidenceScore:0.##}, placeId={PlaceId}, match={MatchType}";
}