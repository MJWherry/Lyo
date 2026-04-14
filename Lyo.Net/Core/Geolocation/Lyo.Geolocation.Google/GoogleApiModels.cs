namespace Lyo.Geolocation.Google;

// Google Geocoding API response models
internal class GoogleGeocodeResponse
{
    public string? Status { get; set; }

    public List<GoogleGeocodeResult>? Results { get; set; }

    public string? ErrorMessage { get; set; }
}

internal class GoogleGeocodeResult
{
    public List<GoogleAddressComponent>? AddressComponents { get; set; }

    public string? FormattedAddress { get; set; }

    public GoogleGeometry? Geometry { get; set; }

    public string? PlaceId { get; set; }

    public List<string>? Types { get; set; }
}

internal class GoogleAddressComponent
{
    public string? LongName { get; set; }

    public string? ShortName { get; set; }

    public List<string>? Types { get; set; }
}

internal class GoogleGeometry
{
    public GoogleLocation? Location { get; set; }

    public string? LocationType { get; set; }

    public GoogleBounds? Viewport { get; set; }

    public GoogleBounds? Bounds { get; set; }
}

internal class GoogleLocation
{
    public double Lat { get; set; }

    public double Lng { get; set; }
}

internal class GoogleBounds
{
    public GoogleLocation? Northeast { get; set; }

    public GoogleLocation? Southwest { get; set; }
}

// Google Time Zone API response models
internal class GoogleTimeZoneResponse
{
    public string? Status { get; set; }

    public string? TimeZoneId { get; set; }

    public string? TimeZoneName { get; set; }

    public int? RawOffset { get; set; }

    public int? DstOffset { get; set; }

    public string? ErrorMessage { get; set; }
}

// Google Directions API response models
internal class GoogleDirectionsResponse
{
    public string? Status { get; set; }

    public List<GoogleRoute>? Routes { get; set; }

    public string? ErrorMessage { get; set; }
}

internal class GoogleRoute
{
    public GoogleBounds? Bounds { get; set; }

    public string? Summary { get; set; }

    public List<GoogleLeg>? Legs { get; set; }

    public List<string>? Warnings { get; set; }

    public List<int>? WaypointOrder { get; set; }
}

internal class GoogleLeg
{
    public GoogleDistance? Distance { get; set; }

    public GoogleDuration? Duration { get; set; }

    public GoogleDuration? DurationInTraffic { get; set; }

    public string? StartAddress { get; set; }

    public string? EndAddress { get; set; }

    public GoogleLocation? StartLocation { get; set; }

    public GoogleLocation? EndLocation { get; set; }

    public List<GoogleStep>? Steps { get; set; }
}

internal class GoogleStep
{
    public GoogleDistance? Distance { get; set; }

    public GoogleDuration? Duration { get; set; }

    public GoogleLocation? StartLocation { get; set; }

    public GoogleLocation? EndLocation { get; set; }

    public string? HtmlInstructions { get; set; }

    public string? Instructions { get; set; }

    public string? Maneuver { get; set; }

    public string? RoadName { get; set; }

    public GooglePolyline? Polyline { get; set; }
}

internal class GoogleDistance
{
    public string? Text { get; set; }

    public int Value { get; set; } // Value in meters
}

internal class GoogleDuration
{
    public string? Text { get; set; }

    public int Value { get; set; } // Value in seconds
}

internal class GooglePolyline
{
    public string? Points { get; set; }
}