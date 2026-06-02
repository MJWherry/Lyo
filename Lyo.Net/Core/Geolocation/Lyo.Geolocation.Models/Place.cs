using System.Diagnostics;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class Place
{
    public string PlaceId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public GeoCoordinate Coordinate { get; set; } = null!;

    public Address Address { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Website { get; set; }

    public IEnumerable<string>? Types { get; set; }

    public double? Rating { get; set; }

    public int? UserRatingsTotal { get; set; }

    public string? PriceLevel { get; set; }

    public PlaceOpeningHours? OpeningHours { get; set; }

    public BoundingBox? Viewport { get; set; }

    public override string ToString()
        => $"Place: {Name}, placeId={PlaceId}, {Coordinate}, rating={Rating?.ToString() ?? "n/a"}";
}
