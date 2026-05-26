# Lyo.Api.Models

Shared **HTTP contract** models for Lyo minimal APIs and their **clients**—distinct from [`Lyo.Query.Models`](../../../Data/Query/Lyo.Query.Models/README.md) (filter trees +
projection DTOs).

## Request envelopes

| Area                             | Types (selected)                                                                                                                | Notes                                                                    |
|----------------------------------|---------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------|
| Upsert / update / patch / delete | **`UpsertRequest<T>`**, **`UpdateRequest<T>`**, **`PatchRequest`**, **`DeleteRequest`**, matching fluent **`*Builder` classes** | Mirrors generic CRUD endpoints (server validates path + body alignment). |
| History / exports                | **`HistoryQuery`**, **`ExportRequest`**, **`ExportColumnMapping`**                                                              | Drive bulk read + file generation endpoints.                             |

Builders follow **method-chaining** ergonomics so gateways/tests avoid object initializer noise.

## Response envelopes

Concrete result records emitted by `Lyo.Api` endpoints (see [`Common/Response/Result.cs`](Common/Response/Result.cs) and [`Common/Response/ResultFactory.cs`](Common/Response/ResultFactory.cs)). There is no generic `ApiResponse<T>` / `BulkApiResponse<T>` — each operation has its own typed envelope:

| Envelope                       | Returned by                                       | Notable fields                                                                                                  |
|--------------------------------|---------------------------------------------------|-----------------------------------------------------------------------------------------------------------------|
| **`QueryRes<T>`**              | `POST {route}/Query`                              | `IsSuccess`, `Items`, `Start`, `Amount`, `Total`, `HasMore`, `QueryScore`, `Error` (echoes `QueryRequest`).     |
| **`ProjectedQueryRes<T>`**     | `POST {route}/QueryProject`                       | Adds `EntityTypes` (root + navigation/template CLR class names on success) and echoes the executed `Select`.   |
| **`QueryHistoryResults<T>`**   | `POST {route}/QueryHistory`                       | Wraps an ordered list of `HistoryResult<T>` items with `Start`, `Amount`, `Total`.                              |
| **`HistoryResult<T>`**         | per-row history slice                             | `Value`, `StartTimestamp`, `EndTimestamp`, optional `Error`.                                                    |
| **`CreateResult<T>`** / **`CreateBulkResult<T>`** | Create + Bulk create                  | `IsSuccess` / `Data` / `Error` per row; bulk wraps `CreatedCount` / `FailedCount`.                              |
| **`UpdateResult<T>`** / **`UpdateBulkResult<T>`** | Update + Bulk update                  | `Result` enum (`Updated`/`NoChange`/`Failed`), `Keys`, `OldData`/`NewData`; bulk adds `NoChangeCount`.          |
| **`PatchResult<T>`** / **`PatchBulkResult<T>`** | Patch + Bulk patch                      | `Result` enum, `OldData`/`NewData`, `UpdatedProperties`; `IsSuccess` derived from `Updated`/`NoChange`.         |
| **`UpsertResult<T>`** / **`UpsertBulkResult<T>`** | Upsert + Bulk upsert                  | `Result` enum (`Created`/`Updated`/`NoChange`/`Failed`); bulk includes `Created/Updated/NoChange/FailedCount`.  |
| **`DeleteResult<T>`** / **`DeleteBulkResult<T>`** | Delete + Bulk delete                  | `IsSuccess`, `Data` (deleted row), `Error`.                                                                     |

Use **`ResultFactory.QuerySuccess` / `ProjectedQuerySuccess` / `CreateBulk` / `UpdateBulk` / …`** to build envelopes from CRUD service code — they pre-compute counts, query scores, and split success vs failure paths.

## Other models & metadata

- **`CrudMetadata`** — standardized success payload fragments (timestamps, concurrency tokens if host sets them).
- **`FileUpload` / `FileUploadRes`** — bridge file pipeline results.
- **`CacheItem`** (record + **`CacheItemTypeEnum`** `Key` / `Tag`) — describes server cache entries surfaced by introspection endpoints; helpers `CacheItem.Key(name)` / `CacheItem.Tag(name)` build instances and the record’s `GetHashCode` / `ToString` are stable for set membership.
- **`Constants.ApiErrorCodes`** — single source of truth for stable `errors[].code` strings on problem responses (`Unknown`, `InvalidQuery`, `InvalidField`, `InvalidPaging`, `NotFound`, `Forbidden`, `Cancelled`, `SqlException`, `MessageQueueConnectionIssue`, `Conflict`, `ExceedMaxBulkSize`, etc.). **Check before duplicating** literals in clients.

## Errors & Problem Details

Rather than per-app ad-hoc exceptions, this package standardizes:

- **`LyoProblemDetails`** (record) / **`ILyoProblemDetails`** — RFC 9457 problem details serialized with the default JSON contract; carries `Detail`, `Status`, `Timestamp`, `Errors` (array of **`ApiError`**), `Title`, `Type`, `Instance`, `TraceId`, `SpanId`, optional `Stacktrace`, and bag-of-extensions. Helpers: `MapErrorCodeToHttpStatus(code)` (uses **`Constants.ApiErrorCodes`**), `FromCode(errorCode, detail, …)`, and `HttpStatusTitle(statusCode)` (used by export wrapping).
- **`ApiError`** (record) — single entry in `errors[]`: `Code`, `Description`, optional `Stacktrace`.
- **`ApiErrorException`** — sealed `Exception` carrying a `LyoProblemDetails`; thrown when an operation fails with a structured problem (e.g. export over a failed projected query).
- **`InvalidPropertyNameException`** — thrown when patch keys do not exist on the target type (surfaced as `InvalidPatchRequest`).
- **`LFException`** ([`Error/LFException.cs`](Error/LFException.cs)) — domain-level Lyo exception carrying a stable `ErrorCode`; hosts (e.g. `Lyo.Api`'s `LoggingMiddleware`) translate it into warning-level `LyoProblemDetails`.
- **`LyoProblemDetailsBuilder`** — fluent builder for trace/span/route fields, message and error-code overrides, and `AddApiError` / `FromException` shortcuts.

The same JSON shape round-trips through `ApiClient`'s `ApiException` so error handling stays symmetric on both ends.

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
