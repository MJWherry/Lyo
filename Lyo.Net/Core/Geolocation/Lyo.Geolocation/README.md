# Lyo.Geolocation

Provider-agnostic **geospatial operations** façade.

## Role of this assembly

Defines **`IGeolocationService`** (async surface) referencing shared DTOs from [`Lyo.Geolocation.Models`](../Geolocation.Models/README.md)—**addresses**, **coordinates**, **distance enums**, routing envelopes—while keeping **vendor HTTP/SDK wiring out** of the contract.

Consumers inject **`IGeolocationService`** inside domain logic (shipping radius checks, SLA windows, travel-time estimates, timezone-aware reminders) **without referencing** Google/AWS-specific types.

## Implementations shipped in-tree

| Package | Responsibility |
|---------|----------------|
| [`Lyo.Geolocation.Google`](../Lyo.Geolocation.Google/README.md) | REST-backed **`GoogleGeolocationService`** wrapping Google Maps platform endpoints (Directions, Time Zone API, …). |

Add more providers (`MapboxGeolocationService`, `AzureMapsGeolocationService`, …) in separate assemblies that depend on **Models + this abstraction** only.

### Interface capabilities (mental map)

The contract groups operations into thematic areas:

**Geocode / reverse**

- Convert **street strings** or structured **`Address`** objects into **`GeoCoordinate`**.
- **`GeocodeBatchAsync`** fans out parallel requests—still subject to quotas; backoff belongs in implementations.
- Reverse paths resolve coordinates back into normalized **`Address`** graphs (localized components handled in models).

**Distance & vicinity**

- **Spherical distances** versus **routing-derived driving distances** (see overloads distinguishing **straight-line** vs **routing** APIs).
- **`IsWithinRadiusAsync`** answers membership questions for alerting (“nearest technician within 40 km”), typically using great‑circle math unless overridden.

**Timezone**

**`GetTimeZoneAsync`** retrieves **IANA zone ids** (“America/New_York”) powering scheduling + audit timelines.

**Routes**

**`Route` / `RouteOptions`** exposes multi-leg durations, geometries (if providers enrich), modality selection (consult models for supported enum values relevant to integrated provider capabilities).

Every method returns **`Task<…>`** and should honor **`CancellationToken`** when surfaced by implementors—even if abstraction interface omits overloads, concrete Google service uses cooperative cancellation on HTTP requests.

### Design constraints

Abstraction purposely **does not** standardize caching—add memoization decorators in your composition root if repeatedly geocoding identical warehouse addresses (`IDecorator` pattern preventing thundering herds on cold starts).

**Security / privacy**: avoid logging raw user-provided addresses in infrastructure logs unless compliant with policy—wrap provider logging filters upstream.

Telemetry hooks live in implementations (metrics around quota exhaustion, durations per external API)—not mandated here.

## Extension guidance for new vendors

Implement **`IGeolocationService`** end-to-end, map provider-specific status codes → domain exceptions sparingly (**prefer fault types** centralized in hosting layer).

Share DTO converters in **Models** assembly when multiple providers ingest same logical shape (prevent drift between Google vs competitor address normalization).

Use dependency injection **`IOptions<TProviderOptions>`** for API keys/regions—not static configuration.

## See also

[`Lyo.Geolocation.Models`](../Geolocation.Models/README.md) — enumerations (`DistanceUnit`, `TransportMode`), coordinate math helpers, postal normalization records.
