# Lyo.Result

`Result` / `Result<T>` and related types. Separate from [`Lyo.Common`](../../Common/Lyo.Common/README.md) `Result` (different namespace and design). Feature libraries pick `Lyo.Result` when they want `Error` graphs, builders, bulk/paged envelopes, and `Task` composition without pulling the whole API layer.

## `Result<T>` and `IResult<T>`

- `IsSuccess`, `Data`, `Errors`, `Timestamp`, `Metadata` (optional key/value bag on `ResultBase`).
- **Factory methods.** `Result<T>.Success(...)`, `Failure(IReadOnlyList<Error>)`, `Failure(Error)`, `Failure(message, code, …)`, `Failure(Exception, …)`.
- **Unwrap / extract.** `TryGetValue`, `ValueOrThrow`, `ValueOrDefault`, `Match`, `Map` / `MapAsync`, `Tap` side effects, boolean operators where defined on the record.

## `Error`

`Error` is an immutable record with `Message`, `Code`, `Severity`, and `Type` (`ErrorType`: Generic, Validation, NotFound, Conflict, Unauthorized, and more). It also has `StackTrace`, `Exception`, `InnerError` (chained errors mimic exception chains), `Metadata`, and `Timestamp`. Factory helpers on `Error` include `FromException`, `Validation`, `NotFound`, `Conflict`, and `Unauthorized`, so call sites do not hand-roll severity/type for common cases. Validation-specific codes often flow through `ValidationErrorCodes`.

## Void and non-data success

- `ResultVoid` covers operation completed, no payload, alongside `Result<T>` (see `ResultVoid.cs`).
- `Unit` is a typed no-value placeholder where APIs want `Result<Unit>`.

## `Option<T>`

Optional presence without treating no value as a failed operation. Distinct from `Result`. Use `Option` when absence is not an error (e.g. optional query row).

## Builders

- **ResultBuilder<T>.** Fluent `WithSuccess` / `WithFailure` / `AddError` / `WithMetadata` / `Build()`.
- **BulkResultBuilder.** Accumulate many item-level outcomes into `BulkResult`.
- **ErrorBuilder.** Compose `Error` graphs (nested inner errors, metadata).

## Request-paired results

- **Result<TRequest, TResult>.** Extends `Result<TResult>` with the original `TRequest` payload, so failure paths can echo back the input that produced the error. Adds `TryGetRequest`, a four-tuple `Deconstruct`, and request-aware `Success` / `Failure(Exception, …)` factories.
- **BulkResult<TRequest, TResult>.** Bulk variant whose `Results`, `SuccessfulResults`, and `FailedResults` collections are paired (`Result<TRequest, TResult>`), plus `SuccessfulRequests` / `FailedRequests` projections for re-driving partial failures.
- **BulkResultFromRequest<TRequest, TResult>.** A single request that expands into many per-item results (e.g. one upload to many row outcomes), with `FromData`, `FromResults`, `FromErrors`, and `FromException` helpers.

## Lists and paging

- **BulkResult<T>.** Many operations in one round-trip with cached `SuccessCount` / `FailureCount`, `IsCompleteSuccess` / `IsCompleteFailure` / `HasPartialSuccess` flags, `ErrorCodes` / `ErrorMessages` (flattened over inner errors), `SuccessfulData` / `FailedData`, and `FromResults` / `FromData` / `FromErrors` factories.
- **PagedResult.** Page metadata plus items as a `Result` envelope.

## Async composition

`AsyncResultExtensions` provides `ThenAsync` (chain `Task<Result<…>>` only on success), `OnSuccessAsync` / `OnFailureAsync`, overloads that propagate `CancellationToken`, and adapters from `Task` plus exceptions into `Result`. Use these to keep async pipelines linear without nested `if (!result.IsSuccess) return …`.

## Guards and validation

`Ensure` and `ValidationHelpers` express preconditions and collect validation failures into `Error` / `Result` shapes (see XML docs on each file).

## Exception to Result adapters

`ExceptionExtensions` adapters for exception-throwing code:

| Extension | Purpose |
| ----------------------------------------- | ---------------------------------------------------------------------------------------- |
| `Exception.ToResult<T>(code?)` | Wraps an exception as `Result<T>.Failure(exception, code)`. |
| `Task<T>.ToResultAsync<T>(code?)` | Awaits the task; success → `Result<T>.Success`, exception → `Result<T>.Failure`. |
| `Task<Result<T>>.ToResultAsync<T>(code?)` | Awaits a result-returning task and converts thrown exceptions into a failed `Result<T>`. |

## Logging

`ResultLoggingExtensions` attach log scopes for success and failure that carry the same fields your logging config can correlate.

## Regex / infrastructure

`RegexPatterns` hosts shared validation patterns used by higher layers (emails, route slugs, and similar). Check call sites before assuming a specific regex is the product rule.

## Relationship to encryption and HTTP

- `Lyo.Encryption` ships `EncryptionResult` / `DecryptionResult` models in this namespace for operations that want `Result` without throwing for routine failure modes.
- Translation and SMS envelope stacks reference `Lyo.Result` for `Error` typing in their public models.

## When to choose this vs `Lyo.Common.Result`

- Prefer `Lyo.Result` when you need multiple errors, severity/type, bulk/paged wrappers, or async `ThenAsync` chains.
- Prefer `Lyo.Common` primitives when you are only inside code that already standardized on that `Result` type and you do not want two result types in the same boundary.

## See also

- [`Lyo.Validation`](../../Validation/Lyo.Validation/README.md). Often returns structured failures compatible with richer error handling in hosts.
- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md). Guard helpers shared with validation and keystore stacks.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)