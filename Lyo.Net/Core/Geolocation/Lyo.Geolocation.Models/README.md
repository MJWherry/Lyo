# Lyo.Geolocation.Models

Neutral data contracts shared by the **`Lyo.Geolocation`** abstraction and concrete providers (e.g. [`Lyo.Geolocation.Google`](../Lyo.Geolocation.Google/README.md)).

## DTO catalog

**Coordinates & geometry**

| Type | Role |
|------|------|
| **`Coordinates.GeoCoordinate`** | Latitude / longitude (+ optional altitude, accuracy, timestamp). Validates lat in `[-90, 90]` and lon in `[-180, 180]` via property setters. |
| **`BoundingBox`** | Southwest/Northeast pair with `Center`/`Northwest`/`Southeast` accessors, `Contains`, `Intersects`, `Expand(meters)`, `GetWidth/Height/Area`, and `FromCenterAndRadius` factory. |

**Addresses**

| Type | Role |
|------|------|
| **`Addresses.Address`** | Unified US + international address with structured street/unit/locality/postal/country fields, `GetFormattedAddress(AddressFormat)`, `Normalize()`, `IsValid()`, `IsComplete()`, `GetCanonicalForm()`, `IsSimilarTo(...)`, and `FromComponents` / `CreateUSAddress` / `CreateInternationalAddress` factories. |
| **`Addresses.ContactAddress`** | Person ↔ address junction with `ContactAddressType`, primary flag, start/end dates. |
| **`Extensions.AddressExtensions`** | `IsInUnitedStates`, `GetStateAbbreviation`, `ToMailingFormat`, `GetPostalCode`, `GetStateOrProvince`. |
| **`AddressFormat`** / **`AddressType`** / **`AddressValidationStatus`** (in `Addresses.Address.cs`) | Formatting variant, classification, and validation lifecycle. |

**Geocoding & places**

| Type | Role |
|------|------|
| **`GeocodeOptions`** | Language, region bias, optional bounds, `MaxResults`, component restrictions. |
| **`GeocodeResult`** / **`GeocodeResultItem`** / **`GeocodeMatchType`** | Single forward-geocode hit, batch row, and match-quality enum. |
| **`ReverseGeocodeResult`** | Address(es) + confidence for a coordinate. |
| **`BatchGeocodeResult`** | Aggregate totals + per-item results + processing time. |
| **`Place`** / **`PlaceOpeningHours`** / **`PlaceHoursPeriod`** | POI with id, name, coordinate, address, rating, hours. |
| **`ProximitySearchResult<T>`** | Wraps an arbitrary `T` with `DistanceMeters` and km/mile conversions. |

**Routing**

| Type | Role |
|------|------|
| **`Route`** | Route id, start/end, waypoints, steps, total distance, estimated duration (+ traffic), transport mode, bounding box, summary, warnings. |
| **`RouteStep`** | Per-leg distance, duration, instructions, road name, `ManeuverType`. |
| **`RouteOptions`** | Mode, avoid tolls/highways/ferries, waypoints + optimization, departure/arrival time, language, units. |

**Distance / time zones / IP**

| Type | Role |
|------|------|
| **`DistanceResult`** | Source/destination coordinates + meters (with km/mile conversions) + `DistanceCalculationMethod`. |
| **`GeoTimeZone`** | IANA `TimeZoneId`, display name, UTC offset, DST offset, DST start/end. |
| **`IpGeolocationResult`** | IP-to-location payload (city/region/country, ISP, proxy/VPN flags, accuracy radius). |
| **`ValidationIssue`** / **`ValidationWarning`** | Severity-tagged validation findings for address/geocoding flows. |

**Enums (under `Enums/`)**

- **`DistanceUnit`** — `Meters`, `Kilometers`, `Miles`, `Feet`, `NauticalMiles`.
- **`DistanceCalculationMethod`** — `Haversine`, `Vincenty`, `Driving`, `Walking`, `Bicycling`.
- **`TransportMode`** — `Driving`, `Walking`, `Bicycling`, `Transit`, `Flying`.
- **`ManeuverType`** — turn / ramp / merge / fork / roundabout / ferry / arrive set.
- **`GeocodingAccuracy`** — `Rooftop`, `RangeInterpolated`, `GeometricCenter`, `Approximate`.
- **`ContactAddressType`** — `Home`, `Work`, `Billing`, `Shipping`, `Mailing`, `Other`.
- **`AddressType`** (in `Addresses/Address.cs`) — `Residential`, `Commercial`, `POBox`, `Military`, `Other`.
- **`ValidationSeverity`** — `Info`, `Warning`, `Error`, `Critical`.

## Geometry helpers

- **`GeoCoordinate.DistanceTo(other, DistanceUnit unit = Meters)`** uses the **Haversine** great-circle formula with Earth radius 6 371 000 m; pick the desired unit via the
  enum rather than dividing afterwards.
- **`GeoCoordinate.IsWithinRadius(center, radiusMeters)`** and **`GeoCoordinate.Offset(metersNorth, metersEast)`** are built on the same primitive.
- **`GeoCoordinate.ToDms()`** prints `D°M'S"N/S D°M'S"E/W`; `FromDms` is not yet implemented (`NotImplementedException`).
- **`BoundingBox.Expand(meters)`** and **`BoundingBox.FromCenterAndRadius(center, radiusMeters)`** use the flat-Earth approximation (`111 320 m` per degree latitude, adjusted by
  `cos(latitude)` for longitude); `GetWidth`/`GetHeight` delegate to `GeoCoordinate.DistanceTo` for actual measured values.
- **`DistanceResult.Method`** records whether the number came from a geodesic formula or a routing provider so UI copy can disclose fidelity.

## Why split models?

Keeps **`IGeolocationService`** binary light for consumers that only need data contracts (e.g. Blazor WASM clients can reference the models without pulling HTTP stacks).

Centralizes serialization stability so each provider reuses the same JSON shapes instead of re-declaring DTOs.

When evolving the shapes:

1. Prefer **additive fields** (`init`/`required` thoughtfully).
2. For breaking changes, ship a parallel type (e.g. `AddressV2`) instead of silently changing semantics.

## Dependencies

*(Synchronized from `Lyo.Geolocation.Models.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- [`Lyo.Common`](../../Common/Lyo.Common/README.md)
