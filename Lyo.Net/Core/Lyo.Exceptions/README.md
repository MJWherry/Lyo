# Lyo.Exceptions

Custom exception types and argument validation helpers for the Lyo library suite. Used across all Lyo packages for consistent error handling and validation.

## Features

- **ArgumentHelpers** – Argument validation with `ThrowIfNull`, `ThrowIfNullOrWhiteSpace`, `ThrowIfNullOrEmpty`, `ThrowIf`, `ThrowIfNotInRange`, `ThrowIfFileNotFound`,
  `ThrowIfNullReturn` (for constructor chaining)
- **UriHelpers** – URI validation with `GetValidWebUri`, `ThrowIfInvalidUri`, `ThrowIfInvalidAbsoluteUri`
- **OperationHelpers** – Operation/state validation (`ThrowIf`, `ThrowIfNull`, `ThrowIfNullOrWhiteSpace`, stream checks, disposition/cancellation/not-supported) plus numeric parity with
  `ArgumentHelpers`: `ThrowIfNotInRange` (scalar, `DateTime`, `TimeSpan`, array lengths), `ThrowIfGreaterThan`, `ThrowIfGreaterThanOrEqual`, `ThrowIfLessThan`, `ThrowIfLessThanOrEqual`,
  `ThrowIfZero`, sign helpers, `ThrowIfEqual`, `ThrowIfNotEqual`
- **OrThrow / StringOr** – Fluent `string?.Or` / `.OrIfWhiteSpace` chains and terminals (`OrThrowInvalidOperation`, `OrThrow(Func<…>)`, etc.) plus generic `OrThrowInvalidOperation`/`OrThrow`
  unwraps—implemented here so callers do not pull in `Lyo.Common` for throw-only ergonomics
- **FileHelpers** – File name validation with `ThrowIfFileNameInvalid`, `GetValidFileName`, and path-prefix safety helpers `NormalizePathPrefix`, `ThrowIfPathPrefixTraversal`, `NormalizeAndValidatePathPrefix` (shared by every Lyo.FileStorage backend's save/direct-upload/diagnostics paths)
- **FormatHelpers** – Format validation throwing `InvalidFormatException` (`ThrowIfInvalidGuid`, `GetValidGuid`, hex color, `ThrowIfInvalidFormat` with custom regex, etc.)
- **ExceptionThrower** – File/directory existence and accessibility (`ThrowIfDirectoryNotFound`, `ThrowIfFileNotAccessible`, `ThrowIfDirectoryNotAccessible`)
- **Custom exceptions** – `ArgumentOutsideRangeException`, `InvalidFormatException`, `NotFoundException`, `HttpException` (base for `UnauthorizedException`, `ForbiddenException`,
  `ConflictException`, `NotFoundException`, `RateLimitExceededException`, `ServiceUnavailableException`), and more

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
```

## Dependencies

*(Synchronized from `Lyo.Exceptions.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

*None.*
