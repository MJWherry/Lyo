using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;

namespace Lyo.Geolocation.Models;

public class ReverseGeocodeResult
{
    public GeoCoordinate Coordinate { get; set; }

    public Address Address { get; set; }

    public double ConfidenceScore { get; set; }

    public string PlaceId { get; set; }

    public IEnumerable<Address> AlternativeAddresses { get; set; } // Multiple matches

    public IDictionary<string, string> Metadata { get; set; }
}