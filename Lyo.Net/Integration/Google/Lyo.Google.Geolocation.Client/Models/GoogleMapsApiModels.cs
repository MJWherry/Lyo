namespace Lyo.Google.Geolocation.Client.Models;

internal sealed class GoogleGeocodeResponse
{
    public string? Status { get; set; }

    public List<GoogleGeocodeResult>? Results { get; set; }

    public string? ErrorMessage { get; set; }
}

internal sealed class GoogleGeocodeResult
{
    public List<GoogleAddressComponent>? AddressComponents { get; set; }

    public string? FormattedAddress { get; set; }

    public GoogleGeometry? Geometry { get; set; }

    public string? PlaceId { get; set; }

    public List<string>? Types { get; set; }
}

internal sealed class GoogleAddressComponent
{
    public string? LongName { get; set; }

    public string? ShortName { get; set; }

    public List<string>? Types { get; set; }
}

internal sealed class GoogleGeometry
{
    public GoogleLocation? Location { get; set; }

    public string? LocationType { get; set; }

    public GoogleBounds? Viewport { get; set; }

    public GoogleBounds? Bounds { get; set; }
}

internal sealed class GoogleLocation
{
    public double Lat { get; set; }

    public double Lng { get; set; }
}

internal sealed class GoogleBounds
{
    public GoogleLocation? Northeast { get; set; }

    public GoogleLocation? Southwest { get; set; }
}

internal sealed class GoogleTimeZoneResponse
{
    public string? Status { get; set; }

    public string? TimeZoneId { get; set; }

    public string? TimeZoneName { get; set; }

    public int? RawOffset { get; set; }

    public int? DstOffset { get; set; }

    public string? ErrorMessage { get; set; }
}

internal sealed class GoogleDirectionsResponse
{
    public string? Status { get; set; }

    public List<GoogleRoute>? Routes { get; set; }

    public string? ErrorMessage { get; set; }
}

internal sealed class GoogleRoute
{
    public GoogleBounds? Bounds { get; set; }

    public string? Summary { get; set; }

    public List<GoogleLeg>? Legs { get; set; }

    public List<string>? Warnings { get; set; }
}

internal sealed class GoogleLeg
{
    public GoogleDistance? Distance { get; set; }

    public GoogleDuration? Duration { get; set; }

    public GoogleDuration? DurationInTraffic { get; set; }

    public GoogleLocation? StartLocation { get; set; }

    public GoogleLocation? EndLocation { get; set; }

    public List<GoogleStep>? Steps { get; set; }
}

internal sealed class GoogleStep
{
    public GoogleDistance? Distance { get; set; }

    public GoogleDuration? Duration { get; set; }

    public GoogleLocation? StartLocation { get; set; }

    public GoogleLocation? EndLocation { get; set; }

    public string? HtmlInstructions { get; set; }

    public string? Instructions { get; set; }

    public string? Maneuver { get; set; }

    public string? RoadName { get; set; }
}

internal sealed class GoogleDistance
{
    public int Value { get; set; }
}

internal sealed class GoogleDuration
{
    public int Value { get; set; }
}
