using System.Diagnostics;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class ReverseGeocodeResult
{
    public GeoCoordinate Coordinate { get; set; } = null!;

    public Address Address { get; set; } = null!;

    public double ConfidenceScore { get; set; }

    public string PlaceId { get; set; } = string.Empty;

    public IEnumerable<Address>? AlternativeAddresses { get; set; }

    public IDictionary<string, string>? Metadata { get; set; }

    public override string ToString()
        => $"ReverseGeocodeResult: {Coordinate}, {Address}, conf={ConfidenceScore:0.##}, placeId={PlaceId}";
}
