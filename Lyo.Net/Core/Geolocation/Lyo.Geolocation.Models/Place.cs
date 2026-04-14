using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;

namespace Lyo.Geolocation.Models;

public class Place
{
    public string PlaceId { get; set; }

    public string Name { get; set; }

    public GeoCoordinate Coordinate { get; set; }

    public Address Address { get; set; }

    public string PhoneNumber { get; set; }

    public string Website { get; set; }

    public IEnumerable<string> Types { get; set; } // restaurant, bank, etc.

    public double? Rating { get; set; }

    public int? UserRatingsTotal { get; set; }

    public string PriceLevel { get; set; }

    public PlaceOpeningHours OpeningHours { get; set; }

    public BoundingBox Viewport { get; set; }
}