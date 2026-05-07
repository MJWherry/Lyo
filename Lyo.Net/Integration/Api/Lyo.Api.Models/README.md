# Lyo.Api.Models

Shared **HTTP contract** models for Lyo minimal APIs and their **clients**—distinct from [`Lyo.Query.Models`](../../../Data/Query/Lyo.Query.Models/README.md) (filter trees +
projection DTOs).

## Request envelopes

| Area                             | Types (selected)                                                                                                                | Notes                                                                    |
|----------------------------------|---------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------|
| Upsert / update / patch / delete | **`UpsertRequest<T>`**, **`UpdateRequest<T>`**, **`PatchRequest`**, **`DeleteRequest`**, matching fluent **`*Builder` classes** | Mirrors generic CRUD endpoints (server validates path + body alignment). |
| History / exports                | **`HistoryQuery`**, **`ExportRequest`**, **`ExportColumnMapping`**                                                              | Drive bulk read + file generation endpoints.                             |

Builders follow **method-chaining** ergonomics so gateways/tests avoid object initializer noise.

## Responses & metadata

- **`CrudMetadata`** — standardized success payload fragments (timestamps, concurrency tokens if host sets them).
- **`FileUpload` / `FileUploadRes`** — bridge file pipeline results.
- **`Constants`** — shared header names, discriminator strings, etc.—**check before duplicating** literals in clients.

## Errors & Problem Details

Rather than per-app ad-hoc exceptions, this package standardizes:

**`LyoProblemDetails` / `ILyoProblemDetails`**, **`LFException`**, **`ApiError*` / `ApiErrorException`**

…so both **server ProblemDetails serializer** and **`ApiClient`** can parse consistent JSON (`type/title/status/detail/extensions` mapping—see source for versioned fields).

`LyoProblemDetailsBuilder` simplifies constructing rich machine-readable errors (field-level validation arrays, trace ids).

## Caching & diagnostics

**`CacheItem` + `CacheItemTypeEnum`** describe server cache introspection endpoints (used by internal tools & load tests).

**`QueryRequestScorer`**, **`QueryRequestScoreBreakdown`** support explainability endpoints around query planning (pair with scoring fields on `ResultFactory` outputs + server
logs).

## Relationship to `Lyo.Result`

Some server modules choose [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md) internally; **ProblemDetails** remain the **wire** representation while `Lyo.Result` informs *
*domain** logic in process boundaries.

## Versioning guidance

These DTOs intentionally track **public API JSON**—changing property names is a **semver/breaking HTTP** concern. When evolving:

1. Add **`[JsonPropertyName]`** compatibility shims during transition windows.
2. Document dual-read servers before removing legacy names.
3. Prefer additive optional properties over reinterpretation of existing ones.

## Consumers

- Minimal API hosts (**`Lyo.Api`**) reference these types directly in endpoint signatures.
- Remote workers use the same models with [`Lyo.Api.Client`](../Lyo.Api.Client/README.md) to eliminate translation layers.
