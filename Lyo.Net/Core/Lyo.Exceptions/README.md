# Lyo.Exceptions

Custom exception types and argument validation helpers for the Lyo library suite. Used across all Lyo packages for consistent error handling and validation.

## Features

- **ArgumentHelpers** – Argument validation with `ThrowIfNull`, `ThrowIfNullOrWhiteSpace`, `ThrowIfNullOrEmpty`, `ThrowIf`, `ThrowIfNotInRange`, `ThrowIfFileNotFound`,
  `ThrowIfNullReturn` (for constructor chaining)
- **UriHelpers** – URI validation with `GetValidWebUri`, `ThrowIfInvalidUri`, `ThrowIfInvalidAbsoluteUri`
- **OperationHelpers** – Operation state validation with `ThrowIf`, `ThrowIfNull`, `ThrowIfNullOrWhiteSpace`, `ThrowIfNullOrEmpty`, `ThrowIfNotReadable`, `ThrowIfNotWritable`
- **FileHelpers** – File name validation with `ThrowIfFileNameInvalid`, `GetValidFileName`
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
    ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
    ArgumentHelpers.ThrowIfNullOrEmpty(data, nameof(data));
    // ...
}

// Constructor chaining
public MyService(IOptions options, ILogger logger)
    : base(ArgumentHelpers.ThrowIfNullReturn(options, nameof(options)), logger)
{
}

// URI validation
var uri = UriHelpers.GetValidWebUri(url, nameof(url));

// Operation state (builder/build validation)
OperationHelpers.ThrowIfNull(_data, "Data must be specified using WithData()");
OperationHelpers.ThrowIf(_count == 0, "At least one item is required");
```

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Exceptions.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

*None.*

## Public API (generated)

Top-level `public` types in `*.cs` (*16*). Nested types and file-scoped namespaces may omit some entries.

- `ArgumentHelpers`
- `ArgumentOutsideRangeException`
- `ConflictException`
- `ExceptionThrower`
- `FileHelpers`
- `ForbiddenException`
- `FormatHelpers`
- `HttpException`
- `InvalidFormatException`
- `NotFoundException`
- `OperationHelpers`
- `RateLimitExceededException`
- `ServiceUnavailableException`
- `UnauthorizedException`
- `UriHelpers`
- `ValidationException`

<!-- LYO_README_SYNC:END -->

