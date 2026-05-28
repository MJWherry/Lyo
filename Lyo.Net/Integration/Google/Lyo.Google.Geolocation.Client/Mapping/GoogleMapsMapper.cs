using Lyo.Common.Enums;
using Lyo.EntityReference.Models;
using Lyo.Geolocation.Models;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;
using Lyo.Google.Geolocation.Client.Models;

namespace Lyo.Google.Geolocation.Client.Mapping;

internal static class GoogleMapsMapper
{
    public const string ProviderName = "google";

    public static GeocodeResult ToGeocodeResult(GoogleGeocodeResult googleResult)
    {
        var address = ToAddress(googleResult);
        var location = googleResult.Geometry?.Location ?? throw new InvalidOperationException("Missing location in geocode result");
        var coordinate = new GeoCoordinate(location.Lat, location.Lng);
        address.Coordinate = coordinate;
        return new GeocodeResult {
            Address = address,
            Coordinate = coordinate,
            ConfidenceScore = MapConfidence(googleResult.Geometry?.LocationType),
            Accuracy = MapAccuracy(googleResult.Geometry?.LocationType),
            MatchType = GeocodeMatchType.Exact,
            PlaceId = googleResult.PlaceId ?? string.Empty,
            Metadata = new Dictionary<string, string>()
        };
    }

    public static ReverseGeocodeResult ToReverseGeocodeResult(GoogleGeocodeResult googleResult, GeoCoordinate coordinate)
    {
        var address = ToAddress(googleResult);
        address.Coordinate = coordinate;
        return new ReverseGeocodeResult {
            Coordinate = coordinate,
            Address = address,
            ConfidenceScore = MapConfidence(googleResult.Geometry?.LocationType),
            PlaceId = googleResult.PlaceId ?? string.Empty,
            AlternativeAddresses = [],
            Metadata = new Dictionary<string, string>()
        };
    }

    public static Address ToAddress(GoogleGeocodeResult googleResult)
    {
        var addressComponents = googleResult.AddressComponents ?? [];
        var address = new Address {
            Coordinate = googleResult.Geometry?.Location != null
                ? new GeoCoordinate(googleResult.Geometry.Location.Lat, googleResult.Geometry.Location.Lng)
                : null,
            FullAddress = googleResult.FormattedAddress,
            GeocodeConfidence = MapConfidence(googleResult.Geometry?.LocationType)
        };

        if (!string.IsNullOrWhiteSpace(googleResult.PlaceId)) {
            address.Sources.Add(new EntitySourceRecord(
                EntityRef.ForKey(GoogleGeolocationSourceTypes.GoogleMapsPlace, googleResult.PlaceId),
                DateTime.UtcNow));
        }

        foreach (var component in addressComponents) {
            var types = component.Types ?? [];
            if (types.Contains("street_number"))
                address.HouseNumber = component.LongName;
            else if (types.Contains("route"))
                address.StreetName = component.LongName;
            else if (types.Contains("locality"))
                address.City = component.LongName;
            else if (types.Contains("administrative_area_level_1"))
                address.State = component.ShortName;
            else if (types.Contains("postal_code")) {
                address.Zipcode = component.LongName;
                address.PostalCode = component.LongName;
            }
            else if (types.Contains("country") && Enum.TryParse<CountryCode>(component.ShortName, out var code))
                address.CountryCode = code;
            else if (types.Contains("sublocality") || types.Contains("neighborhood"))
                address.SubLocality = component.LongName;
            else if (types.Contains("administrative_area_level_2"))
                address.County = component.LongName;
        }

        if (string.IsNullOrEmpty(address.StreetAddress) && !string.IsNullOrEmpty(googleResult.FormattedAddress)) {
            var parts = googleResult.FormattedAddress.Split(',');
            if (parts.Length > 0)
                address.StreetAddress = parts[0].Trim();
        }

        if (string.IsNullOrEmpty(address.StreetAddress))
            address.StreetAddress = googleResult.FormattedAddress;

        return address;
    }

    public static Route ToRoute(GoogleRoute googleRoute, GeoCoordinate start, GeoCoordinate end, RouteOptions options)
    {
        var route = new Route {
            Id = Guid.NewGuid(),
            StartPoint = start,
            EndPoint = end,
            TransportMode = options.Mode,
            Summary = googleRoute.Summary ?? string.Empty,
            Warnings = googleRoute.Warnings ?? []
        };

        var leg = googleRoute.Legs?.FirstOrDefault();
        if (leg != null) {
            route.TotalDistanceMeters = leg.Distance?.Value ?? 0;
            route.EstimatedDuration = TimeSpan.FromSeconds(leg.Duration?.Value ?? 0);
            if (leg.DurationInTraffic != null)
                route.DurationInTraffic = TimeSpan.FromSeconds(leg.DurationInTraffic.Value);

            if (leg.Steps != null) {
                route.Steps = leg.Steps.Select((step, index) => new RouteStep {
                        StepNumber = index + 1,
                        StartLocation = step.StartLocation != null ? new GeoCoordinate(step.StartLocation.Lat, step.StartLocation.Lng) : start,
                        EndLocation = step.EndLocation != null ? new GeoCoordinate(step.EndLocation.Lat, step.EndLocation.Lng) : end,
                        DistanceMeters = step.Distance?.Value ?? 0,
                        Duration = TimeSpan.FromSeconds(step.Duration?.Value ?? 0),
                        Instructions = step.HtmlInstructions ?? step.Instructions ?? string.Empty,
                        RoadName = step.RoadName ?? string.Empty,
                        Maneuver = MapManeuver(step.Maneuver)
                    })
                    .ToList();
            }

            if (leg.StartLocation != null && leg.EndLocation != null) {
                var swLat = Math.Min(leg.StartLocation.Lat, leg.EndLocation.Lat);
                var swLng = Math.Min(leg.StartLocation.Lng, leg.EndLocation.Lng);
                var neLat = Math.Max(leg.StartLocation.Lat, leg.EndLocation.Lat);
                var neLng = Math.Max(leg.StartLocation.Lng, leg.EndLocation.Lng);
                route.Bounds = new BoundingBox { Southwest = new GeoCoordinate(swLat, swLng), Northeast = new GeoCoordinate(neLat, neLng) };
            }
        }

        return route;
    }

    private static double MapConfidence(string? locationType)
        => locationType?.ToUpperInvariant() switch {
            "ROOFTOP" => 1.0,
            "RANGE_INTERPOLATED" => 0.8,
            "GEOMETRIC_CENTER" => 0.6,
            "APPROXIMATE" => 0.4,
            var _ => 0.5
        };

    private static GeocodingAccuracy MapAccuracy(string? locationType)
        => locationType?.ToUpperInvariant() switch {
            "ROOFTOP" => GeocodingAccuracy.Rooftop,
            "RANGE_INTERPOLATED" => GeocodingAccuracy.RangeInterpolated,
            "GEOMETRIC_CENTER" => GeocodingAccuracy.GeometricCenter,
            "APPROXIMATE" => GeocodingAccuracy.Approximate,
            var _ => GeocodingAccuracy.Approximate
        };

    private static ManeuverType MapManeuver(string? maneuver)
    {
        if (string.IsNullOrEmpty(maneuver))
            return ManeuverType.Straight;

        return maneuver.ToLowerInvariant() switch {
            "turn-left" => ManeuverType.TurnLeft,
            "turn-right" => ManeuverType.TurnRight,
            "turn-slight-left" => ManeuverType.TurnSlightLeft,
            "turn-slight-right" => ManeuverType.TurnSlightRight,
            "turn-sharp-left" => ManeuverType.TurnSharpLeft,
            "turn-sharp-right" => ManeuverType.TurnSharpRight,
            "uturn-left" or "uturn-right" => ManeuverType.UTurn,
            "straight" => ManeuverType.Straight,
            "ramp-left" => ManeuverType.RampLeft,
            "ramp-right" => ManeuverType.RampRight,
            "merge" => ManeuverType.Merge,
            "fork-left" or "fork-right" => ManeuverType.Fork,
            "keep-left" => ManeuverType.KeepLeft,
            "keep-right" => ManeuverType.KeepRight,
            "roundabout-left" or "roundabout-right" => ManeuverType.Roundabout,
            "ferry" => ManeuverType.Ferry,
            var _ => ManeuverType.Straight
        };
    }
}
