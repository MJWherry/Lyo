# Lyo.Api.Models

Shared HTTP contract models for Lyo minimal APIs and their clients. Distinct from [`Lyo.Query.Models`](../../../Data/Query/Lyo.Query.Models/README.md) (filter trees + projection DTOs).

## Request envelopes

| Area | Types (selected) | Notes |
| -------------------------------- | --------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Upsert / update / patch / delete | `UpsertRequest<T>`, `UpdateRequest<T>`, `PatchRequest`, `DeleteRequest`, matching fluent **`*Builder` classes** | Mirrors generic CRUD endpoints (server validates path + body alignment). |
| Exports | `ExportRequest`, `ExportColumnMapping` | Drive file generation endpoints. |

Builders are fluent. Tests and gateways can skip object initializers.

## Response envelopes

Concrete result records emitted by `Lyo.Api` endpoints (see [`Common/Response/Result.cs`](Common/Response/Result.cs) and [
`Common/Response/ResultFactory.cs`](Common/Response/ResultFactory.cs)). There is no generic `ApiResponse<T>` / `BulkApiResponse<T>`. Each operation has its own typed envelope:

| Envelope | Returned by | Notable fields |
| ----------------------------------------- | ---------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `QueryRes<T>` | `POST {route}/QueryConcrete` | `IsSuccess`, `Items`, `Start`, `Amount`, `Total`, `HasMore`, `QueryScore`, `Error` (echoes `QueryRequest`). |
| `ProjectedQueryRes<T>` | `POST {route}/QueryProject` | Adds `EntityTypes` (root + navigation/template CLR class names on success) and echoes the executed `Select`. |
| `CreateResult<T>` / `CreateBulkResult<T>` | Create + Bulk create | `IsSuccess` / `Data` / `Error` per row; bulk wraps `CreatedCount` / `FailedCount`. |
| `UpdateResult<T>` / `UpdateBulkResult<T>` | Update + Bulk update | `Result` enum (`Updated`/`NoChange`/`Failed`), `Keys`, `OldData`/`NewData`; bulk adds `NoChangeCount`. |
| `PatchResult<T>` / `PatchBulkResult<T>` | Patch + Bulk patch | `Result` enum, `OldData`/`NewData`, `UpdatedProperties`; `IsSuccess` derived from `Updated`/`NoChange`. |
| `UpsertResult<T>` / `UpsertBulkResult<T>` | Upsert + Bulk upsert | `Result` enum (`Created`/`Updated`/`NoChange`/`Failed`); bulk includes `Created/Updated/NoChange/FailedCount`. |
| `DeleteResult<T>` / `DeleteBulkResult<T>` | Delete + Bulk delete | `IsSuccess`, `Data` (deleted row), `Error`. |

Use **`ResultFactory.QuerySuccess` / `ProjectedQuerySuccess` / `CreateBulk` / `UpdateBulk` / …`** to build envelopes from CRUD service code. They pre-compute counts, query scores,
and split success vs failure paths.

## Other models and metadata

- **`CrudMetadata`.** Success payload fragments (timestamps, concurrency tokens if the host sets them).
- **`FileUpload` / `FileUploadRes`.** File pipeline results.
- `CacheItem` (record + `CacheItemTypeEnum` `Key` / `Tag`). Describes server cache entries surfaced by introspection endpoints; helpers `CacheItem.Key(name)` / `CacheItem.Tag(name)` build instances and the record's `GetHashCode` / `ToString` are stable for set membership.
- **`Constants.ApiErrorCodes`.** Stable `errors[].code` strings on problem responses (`Unknown`, `InvalidQuery`, `InvalidField`, `InvalidPaging`, `NotFound`, `Forbidden`, `Cancelled`, `SqlException`, `MessageQueueConnectionIssue`, `Conflict`, `ExceedMaxBulkSize`, and the rest of the enum).

## Errors and problem details

- `LyoProblemDetails` (record) / `ILyoProblemDetails`. RFC 9457 problem details serialized with the default JSON contract; carries `Detail`, `Status`, `Timestamp`, `Errors` (array of `ApiError`), `Title`, `Type`, `Instance`, `TraceId`, `SpanId`, optional `Stacktrace`, and bag-of-extensions. Helpers: `MapErrorCodeToHttpStatus(code)` ( uses `Constants.ApiErrorCodes`), `FromCode(errorCode, detail, …)`, and `HttpStatusTitle(statusCode)` (used by export wrapping).
- `ApiError` (record). Single entry in `errors[]`: `Code`, `Description`, optional `Stacktrace`.
- `ApiErrorException`. Sealed `Exception` carrying a `LyoProblemDetails`; thrown when an operation fails with a structured problem (e.g. export over a failed projected query).
- `InvalidPropertyNameException`. Thrown when patch keys do not exist on the target type (surfaced as `InvalidPatchRequest`).
- `LFException` ([`Error/LFException.cs`](Error/LFException.cs)). Domain-level Lyo exception carrying a stable `ErrorCode`; hosts (e.g. `Lyo.Api`'s `LoggingMiddleware`) translate it into warning-level `LyoProblemDetails`.
- `LyoProblemDetailsBuilder`. Fluent builder for trace/span/route fields, message and error-code overrides, and `AddApiError` / `FromException` shortcuts.

## Caching and diagnostics

`CacheItem` and `CacheItemTypeEnum` describe server cache introspection endpoints (internal tools and load tests). `QueryRequestScorer` and `QueryRequestScoreBreakdown` feed explain endpoints for query planning. Pair them with scoring fields on `ResultFactory` outputs and server logs.

## Relationship to `Lyo.Result`

Some server modules use [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md) in process. ProblemDetails stay the wire contract.

## Versioning guidance

- Add `[JsonPropertyName]` compatibility shims during transition windows.
- Document dual-read servers before removing legacy names.
- Prefer additive optional properties over reinterpretation of existing ones.

## Consumers

- Minimal API hosts (`Lyo.Api`) reference these types directly in endpoint signatures.
- Remote workers send the same models through [`Lyo.Api.Client`](../Lyo.Api.Client/README.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.DateAndTime` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)