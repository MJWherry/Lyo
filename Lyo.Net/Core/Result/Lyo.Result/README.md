# Lyo.Result

`Result` / `Result<T>` and related types. Distinct from [`Lyo.Common.Core`](../../Common/Lyo.Common.Core/README.md) `Result` (different namespace and design). Feature libraries pick `Lyo.Result` when they want `Error` graphs, builders, bulk/paged envelopes, and `Task` composition without pulling the whole API layer.

## `Result<T>` and `IResult<T>`

- `Data`, `IsSuccess`, `Timestamp`, `Errors`, `Metadata` (optional key/value bag on `ResultBase`).
- **Factory methods.** `Result<T>.Success(...)`, `Failure(Error)`, `Failure(IReadOnlyList<Error>)`, `Failure(message, code, …)`, `Failure(Exception, …)`.
- **Unwrap / extract.** `ValueOrThrow`, `TryGetValue`, `ValueOrDefault`, `Match`, `MapAsync` / `Map`, `Tap` side effects, boolean operators where defined on the record.

## `Error`

`Error` is an immutable record with `Code`, `Message`, `Type`, and `Severity` (`ErrorType`: Validation, Generic, Conflict, NotFound, Unauthorized, and more). It also has `Exception`, `StackTrace`, `InnerError` (chained errors mimic exception chains), `Timestamp`, and `Metadata`. Factory helpers on `Error` include `Validation`, `FromException`, `Conflict`, `NotFound`, and `Unauthorized`, so call sites do not hand-roll severity/type for common cases. Validation-specific codes often flow through `ValidationErrorCodes`.

## Success with no data, and void

- `ResultVoid` covers operation completed, no payload, alongside `Result<T>` (see `ResultVoid.cs`).
- `Unit` is a typed no-value placeholder where APIs want `Result<Unit>`.

## `Option<T>`

A missing value is not a failed operation. That is what `Option` is for, and it is not `Result`. Typical case: an optional query row.

## Builders

- **ResultBuilder<T>.** Fluent `WithFailure` / `WithSuccess` / `AddError` / `WithMetadata` / `Build()`.
- **BulkResultBuilder.** Accumulate many item-level outcomes into `BulkResult`.
- **ErrorBuilder.** Compose `Error` graphs (metadata, nested inner errors).

## Results paired with the request

- **Result<TRequest, TResult>.** Extends `Result<TResult>` with the original `TRequest` payload, so failure paths can echo back the input that produced the error. Adds `TryGetRequest`, a four-tuple `Deconstruct`, and request-aware `Failure(Exception, …)` / `Success` factories.
- **BulkResult<TRequest, TResult>.** Bulk variant whose `SuccessfulResults`, `Results`, and `FailedResults` collections are paired (`Result<TRequest, TResult>`), plus `FailedRequests` / `SuccessfulRequests` projections for re-driving partial failures.
- **BulkResultFromRequest<TRequest, TResult>.** A single request that expands into many per-item results (e.g. one upload to many row outcomes), with `FromResults`, `FromData`, `FromException`, and `FromErrors` helpers.

## Paging and lists

- **BulkResult<T>.** Many operations in one round-trip with cached `FailureCount` / `SuccessCount`, `IsCompleteFailure` / `IsCompleteSuccess` / `HasPartialSuccess` flags, `ErrorMessages` / `ErrorCodes` (flattened over inner errors), `FailedData` / `SuccessfulData`, and `FromData` / `FromResults` / `FromErrors` factories.
- **PagedResult.** Items as a `Result` envelope plus page metadata.

## Composition on async paths

`AsyncResultExtensions` provides `ThenAsync` (chain `Task<Result<…>>` only on success), `OnFailureAsync` / `OnSuccessAsync`, overloads that propagate `CancellationToken`, and adapters from `Task` plus exceptions into `Result`. Use these to keep async pipelines linear without nested `if (!result.IsSuccess) return …`.

## Validation and guards

`ValidationHelpers` and `Ensure` express preconditions and collect validation failures into `Result` / `Error` shapes (see XML docs on each file).

## Adapters from exception to Result

Adapters on `ExceptionExtensions` for code that throws:

| Extension | Purpose |
| ----------------------------------------- | ---------------------------------------------------------------------------------------- |
| `Exception.ToResult<T>(code?)` | Wraps an exception as `Result<T>.Failure(exception, code)`. |
| `Task<T>.ToResultAsync<T>(code?)` | Awaits the task; success → `Result<T>.Success`, exception → `Result<T>.Failure`. |
| `Task<Result<T>>.ToResultAsync<T>(code?)` | Awaits a result-returning task and converts thrown exceptions into a failed `Result<T>`. |

## Logging

`ResultLoggingExtensions` attach log scopes for failure and success that carry the same fields your logging config can correlate.

## Infrastructure / regex

`RegexPatterns` hosts shared validation patterns used by higher layers (route slugs, emails, and similar). Check call sites before assuming a specific regex is the product rule.

## How this relates to encryption and HTTP

- `Lyo.Encryption` ships `DecryptionResult` / `EncryptionResult` models in this namespace for operations that want `Result` without throwing for routine failure modes.
- SMS and translation envelope stacks reference `Lyo.Result` for `Error` typing in their public models.

## Related reading

- Often returns structured failures compatible with richer error handling in hosts: [`Lyo.Validation`](../../Validation/Lyo.Validation/README.md).
- Guard helpers shared with keystore and validation stacks: [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)