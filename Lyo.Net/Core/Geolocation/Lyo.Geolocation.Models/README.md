# Lyo.Geolocation.Models

Neutral data contracts shared by **`Lyo.Geolocation`** abstraction and concrete providers ([`Lyo.Geolocation.Google`](../Google/Lyo.Geolocation.Google/README.md)).

**Core shapes**

| Type | Typical role |
|------|---------------|
| **`GeoCoordinate`** | Latitude/longitude pair with validation helpers ensuring ranges & precision expectations for mapping APIs. |
| **`Address`** / nested components (`StreetAddress`, postal metadata, **`AddressNormalization` helpers`) | Canonical structured address graph returned by reverse-geocode or forwarded to providers. |

**Operational enums**

- **`DistanceUnit`**, **`TransportMode`** (driving/transit semantics depend on downstream provider fidelity).
- Enums influencing batching, bounding boxes, locality filters—scan folder `Models/*` via IDE for exhaustive list (fast-moving during provider expansions).

**Routing payloads**

Types such as **`Route`**, **`RouteStep`**, **`RouteOptions`** articulate distance/duration aggregates and per-leg steps—fields may be partially populated when a provider returns degraded data (always null-check before UI binding).

### Why split models?

Keeps **`IGeolocationService`** binary light for consumers referencing only data contracts (Blazor WASM clients can reference **Models** without dragging HTTP stacks).

Allows **serialization stability** (`System.Text.Json` attributes) centralized—providers reuse identically-shaped JSON bridging without duplicating DTO declarations.

When evolving:

1. Prefer **additive fields** (`init`/`required` thoughtfully) across providers.
2. Version breaking shape changes by introducing **`AddressV2`** rather than silently mutating meanings (parallel deploy safety).

### Geography utilities

**Distance math**

- **`GeoCoordinate.DistanceTo`** uses **Haversine** great-circle geometry (units via **`DistanceUnit`**); **`BoundingBox`** helpers expand/measures using the same primitives.
- **`DistanceResult`** exposes **`DistanceCalculationMethod`** (**Haversine / Vincenty / Driving**)—UI copy should cite the method so users know ellipsoidal vs road-network fidelity.

Use provider routing payloads for turn-by-turn or pricing when liability matters; Haversine is fine for coarse proximity.
### Testing

Builders under test projects often spin synthetic **`GeoCoordinate`** + **`Address`** fixtures—keep them immutable records to exploit `with` cloning in assertions.
