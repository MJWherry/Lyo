# Lyo.Result

Railway-oriented **`Result` / `Result<T>`** and related types. This package is **orthogonal to** [`Lyo.Common`](../../Common/Lyo.Common/README.md) **`Result`** (different namespace
and design); many feature libraries pick **`Lyo.Result`** when they want **rich `Error` graphs**, **builders**, **bulk/paged envelopes**, and **`Task` composition** without pulling
the whole API layer.

## Concepts — `Result<T>` and `IResult<T>`

- **`IsSuccess`**, **`Data`**, **`Errors`**, **`Timestamp`**, **`Metadata`** (optional key/value bag on `ResultBase`).
- **Factory methods:** `Result<T>.Success(...)`, `Failure(IReadOnlyList<Error>)`, `Failure(Error)`, `Failure(message, code, …)`, `Failure(Exception, …)`.
- **Unwrap / extract:** `TryGetValue`, `ValueOrThrow`, `ValueOrDefault`, `Match`, `Map` / `MapAsync`, **Tap** side effects, boolean operators where defined on the record.

## Concepts — `Error`

`Error` is an **immutable record** with: - **`Message`**, **`Code`**, **`Severity`**, **`Type`** (`ErrorType`: Generic, Validation, NotFound, Conflict, Unauthorized, …). - **`StackTrace`**, **`Exception`**, **`InnerError`** (chained errors mimic exception chains). - **`Metadata`**, **`Timestamp`**. **Factory helpers** on `Error` include **`FromException`**, **`Validation`**, **`NotFound`**, **`Conflict`**, **`Unauthorized`**, etc., so call sites do not hand-roll severity/type for common cases. Validation-specific codes often flow through **`ValidationErrorCodes`**.

## Concepts — Void and non-data success

- **`ResultVoid`** / patterns for “operation completed, no payload” live alongside `Result<T>` (see `ResultVoid.cs` usage in your solution).
- **`Unit`** represents a typed “no value” placeholder where APIs want `Result<Unit>`.

## Concepts — `Option<T>`

Optional presence without conflating “failed operation” with “no value”. Distinct from **`Result`**: use **`Option`** when absence is **not** an error (e.g. optional query row).

## Concepts — Builders

- **`ResultBuilder<T>`** — fluent **`WithSuccess` / `WithFailure` / `AddError` / `WithMetadata` / `Build()`**.
- **`BulkResultBuilder`** — accumulate many item-level outcomes into **`BulkResult`**.
- **`ErrorBuilder`** — compose complex **`Error`** graphs (nested inner errors, metadata).

## Concepts — Request-paired results

- **`Result<TRequest, TResult>`** — extends `Result<TResult>` with the original `TRequest` payload, so failure paths can echo back the input that produced the error. Adds `TryGetRequest`, a four-tuple `Deconstruct`, and request-aware `Success` / `Failure(Exception, …)` factories.
- **`BulkResult<TRequest, TResult>`** — bulk variant whose `Results`, `SuccessfulResults`, and `FailedResults` collections are paired (`Result<TRequest, TResult>`), plus `SuccessfulRequests` / `FailedRequests` projections for re-driving partial failures.
- **`BulkResultFromRequest<TRequest, TResult>`** — a single request that expands into many per-item results (e.g. one upload → many row outcomes), with `FromData`, `FromResults`, `FromErrors`, and `FromException` helpers.

## Concepts — Lists and paging

- **`BulkResult<T>`** — many operations in one round-trip with cached `SuccessCount` / `FailureCount`, `IsCompleteSuccess` / `IsCompleteFailure` / `HasPartialSuccess` flags, `ErrorCodes` / `ErrorMessages` (flattened over inner errors), `SuccessfulData` / `FailedData`, and `FromResults` / `FromData` / `FromErrors` factories.
- **`PagedResult`** — page metadata + items as a **`Result`** envelope.

## Concepts — Async composition

**`AsyncResultExtensions`** provides **`ThenAsync`** (chain `Task<Result<…>>` only on success), **`OnSuccessAsync` / `OnFailureAsync`**, overloads that propagate * *`CancellationToken`**, and adapters from **`Task`** + exceptions into **`Result`**. Use these to keep **async pipelines** linear without nested `if (!result.IsSuccess) return …` noise.

## Concepts — Guards and validation

**`Ensure`** and **`ValidationHelpers`** express preconditions and collect validation failures into **`Error`** / **`Result`** shapes (see XML docs on each file).

## Concepts — Exception → Result adapters

**`ExceptionExtensions`** provides drop-in adapters for exception-throwing code:

| Extension | Purpose |
| ----------------------------------------- | ---------------------------------------------------------------------------------------- |
| `Exception.ToResult<T>(code?)` | Wraps an exception as `Result<T>.Failure(exception, code)`. |
| `Task<T>.ToResultAsync<T>(code?)` | Awaits the task; success → `Result<T>.Success`, exception → `Result<T>.Failure`. |
| `Task<Result<T>>.ToResultAsync<T>(code?)` | Awaits a result-returning task and converts thrown exceptions into a failed `Result<T>`. |

## Concepts — Logging

**`ResultLoggingExtensions`** attach consistent log scopes for success/failure (**OpenTelemetry/correlation-friendly** when paired with your logging config).

## Concepts — Regex / infrastructure

**`RegexPatterns`** hosts shared validation patterns used by higher layers (emails, route slugs, etc.—check call sites before assuming a specific regex is “the” product rule).

## Relationship to encryption and HTTP

- **`Lyo.Encryption`** ships **`EncryptionResult` / `DecryptionResult`** models in this namespace for operations that want **`Result` without throwing** for routine failure modes.
- Translation and SMS “envelope” stacks reference **`Lyo.Result`** for **`Error` typing** in their public models.

## When to choose this vs `Lyo.Common.Result`

- Prefer **`Lyo.Result`** when you need **multiple errors**, **severity/type**, **bulk/paged wrappers**, or **async `ThenAsync` chains**.
- Prefer **`Lyo.Common`** primitives when you are **only** inside code that already standardized on that **`Result`** surface and you do not want two result types in the same boundary.

## See also

- [`Lyo.Validation`](../../Validation/Lyo.Validation/README.md) — often returns structured failures compatible with richer error handling in hosts.
- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md) — guard helpers shared with validation and keystore stacks.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `System.Text.Json` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)