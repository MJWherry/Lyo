# Lyo.Exceptions

Custom exception types and argument validation helpers for the Lyo library suite. Used across all Lyo packages for consistent error handling and validation.

## Features

- **ArgumentHelpers** – Argument validation with `ThrowIfNull`, `ThrowIfNullOrWhiteSpace`, `ThrowIfNullOrEmpty`, `ThrowIf`, `ThrowIfNotInRange`, `ThrowIfFileNotFound`,
  `ThrowIfNullReturn` (for constructor chaining), plus `ThrowIfEmpty` (GUIDs), `ThrowIfDefault` (structs such as `DateTime`), `ThrowIfNotDefined` (enum casts), and
  `ThrowIfIndexOutOfRange`
- **UriHelpers** – URI validation with `GetValidWebUri`, `ThrowIfInvalidUri`, `ThrowIfInvalidAbsoluteUri`
- **OperationHelpers** – Operation/state validation (`ThrowIf`, `ThrowIfNull`, `ThrowIfNullOrWhiteSpace`, stream checks, disposition/cancellation/not-supported) plus numeric parity
  with
  `ArgumentHelpers`: `ThrowIfNotInRange` (scalar, `DateTime`, `TimeSpan`, array lengths), `ThrowIfGreaterThan`, `ThrowIfGreaterThanOrEqual`, `ThrowIfLessThan`,
  `ThrowIfLessThanOrEqual`,
  `ThrowIfZero`, sign helpers, `ThrowIfEqual`, `ThrowIfNotEqual`
- **OrThrow / StringOr** – Fluent `string?.Or` / `.OrIfWhiteSpace` chains and terminals (`OrThrowInvalidOperation`, `OrThrow(Func<…>)`, etc.) plus generic
  `OrThrowInvalidOperation`/`OrThrow`
  unwraps—implemented here so callers do not pull in `Lyo.Common` for throw-only ergonomics
- **FileHelpers** – File name validation with `ThrowIfFileNameInvalid`, `GetValidFileName`, and path-prefix safety helpers `NormalizePathPrefix`, `ThrowIfPathPrefixTraversal`,
  `NormalizeAndValidatePathPrefix` (shared by every Lyo.FileStorage backend's save/direct-upload/diagnostics paths)
- **FormatHelpers** – Format validation throwing `InvalidFormatException` (`ThrowIfInvalidGuid`, `GetValidGuid`, hex color, `ThrowIfInvalidFormat` with custom regex, etc.)
- **ConfigurationHelpers** – Configuration validation throwing `ConfigurationException` (`ThrowIf`, `ThrowIfNull`, `ThrowIfNullOrWhiteSpace`) with caller-expression setting names
- **ExceptionThrower** – File/directory existence and accessibility (`ThrowIfDirectoryNotFound`, `ThrowIfFileNotAccessible`, `ThrowIfDirectoryNotAccessible`)
- **Custom exceptions** – `ArgumentOutsideRangeException`, `InvalidFormatException`, `ConfigurationException`, `HttpException` (base for `BadRequestException`,
  `UnauthorizedException`, `ForbiddenException`, `NotFoundException`, `ConflictException`, `GoneException`, `UnprocessableEntityException`, `RateLimitExceededException`,
  `ServiceUnavailableException`, `GatewayTimeoutException`), and more
- **HttpException extras** – every `HttpException` carries an optional machine-readable `ErrorCode` (init-only) and an `IsTransient` flag (true for 429/503/504) that retry
  policies can use as a single predicate; `HttpExceptions.FromStatusCode(statusCode, message)` maps raw status codes to the dedicated types (or `GenericHttpException` for
  unknown codes), and `NotFoundException`/`ConflictException`/`ForbiddenException`/`GoneException` expose `ForResource(name, id)` factories that always set the resource
  properties (avoiding the `new NotFoundException("User", null)` constructor-overload trap)
- **ValidationErrorsBuilder** – accumulates field-level errors (`Add`, `AddIf`, `AddRange`) and throws a single `ValidationException` via `ThrowIfAny()` only when errors exist

## Quick Start

```csharp
using Lyo.Exceptions;

// Argument validation
public void Process(string name, byte[] data)
{
    ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
    ArgumentHelpers.ThrowIfNullOrEmpty(data);
    // ...
}

// Constructor chaining
public MyService(IOptions options, ILogger logger)
    : base(ArgumentHelpers.ThrowIfNullReturn(options), logger)
{
}

// URI validation
var uri = UriHelpers.GetValidWebUri(url);

// Operation state (builder/build validation)
OperationHelpers.ThrowIfNull(_data, "Data must be specified using WithData()");
OperationHelpers.ThrowIf(_count == 0, "At least one item is required");

// New guards
ArgumentHelpers.ThrowIfEmpty(tenantId);              // Guid.Empty
ArgumentHelpers.ThrowIfDefault(createdAt);           // default(DateTime)
ArgumentHelpers.ThrowIfNotDefined((DayOfWeek)value); // undefined enum cast
ArgumentHelpers.ThrowIfIndexOutOfRange(index, items.Count);

// HTTP exceptions with error codes and resource factories
throw NotFoundException.ForResource("User", userId);
throw new ConflictException("Order already submitted.") { ErrorCode = "order.already_submitted" };
var ex = HttpExceptions.FromStatusCode(503, "Upstream unavailable."); // ex.IsTransient == true

// Accumulate validation errors, throw once
var errors = new ValidationErrorsBuilder()
    .AddIf(string.IsNullOrWhiteSpace(req.Email), nameof(req.Email), "Email is required.")
    .AddIf(req.Age < 0, nameof(req.Age), "Age must be positive.");
errors.ThrowIfAny();
```

## Dependencies

*(Synchronized from `Lyo.Exceptions.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

*None.*
