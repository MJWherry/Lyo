# Lyo.Common

Common utilities, extensions, enums, JSON converters, and helper classes for the Lyo library suite.

## Features

- **Ensure** – Validation helpers that return `Result` instead of throwing (NotNull, NotEmpty, InRange, That)
- **Error & ErrorBuilder** – Error record and fluent builder for structured error reporting
- **Result, ResultVoid, BulkResult** – Result types for operation success/failure with error details
- **ResultExtensions** – Combine, FirstSuccess, GetErrorMessages, OnSuccess, OnFailure, Match
- **ResultBuilder** – Fluent builder for constructing Result with multiple errors
- **Utilities** – Hashing (MD5, SHA), property path extraction, `HumanizeFileSize`, `ConvertFromBytes`
- **Disposable** – Dispose pattern helpers
- **CollectionExtensions** – Collection utility extensions
- **Extensions** – Dictionary, enum extensions
- **JSON converters** – Custom `System.Text.Json` converters for enums, DateTime, etc.

## Quick Start

```csharp
using Lyo.Common;
using Lyo.Common.Builders;

// Error handling
var error = ErrorBuilder.Create()
    .WithCode("VALIDATION_ERROR")
    .WithMessage("Invalid input")
    .Build();

// Result type
var result = Result<string>.Success("value");
var failed = Result<string>.Failure("Error message", "CODE");  // message, code
var fromError = Result<string>.Failure(Error.FromException(ex));

// Ensure (returns Result instead of throwing)
var ok = Ensure.NotNull(myValue);
var inRange = Ensure.InRange(count, 1, 100);

// File size helpers (HumanizeFileSize is extension on long)
var readable = sizeBytes.HumanizeFileSize();  // e.g. "1.5 MB"
var mb = Utilities.ConvertFromBytes(sizeBytes, FileSizeUnit.Megabytes);

// Property path from expression
var path = Utilities.GetPropertyPath(x => x.Address.Street); // "Address.Street"
```

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Common.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `System.Text.Json` | `[10,)` |

### Project references

- `Lyo.Exceptions`

## Public API (generated)

Top-level `public` types in `*.cs` (*64*). Nested types and file-scoped namespaces may omit some entries.

- `AsyncResultExtensions`
- `AudioFormat`
- `CardinalDirection`
- `CollectionExtensions`
- `CountryCode`
- `Day`
- `DayFlags`
- `DisabilityStatus`
- `Disposable`
- `EducationLevel`
- `Ensure`
- `ErrorBuilder`
- `ErrorSeverity`
- `Ethnicity`
- `ExceptionExtensions`
- `Extensions`
- `FederalFilingStatus`
- `FileSizeUnit`
- `FileTypeCategory`
- `FileTypeFlags`
- `GenericComparer`
- `HttpStatusCodeCategory`
- `ImageFormat`
- `IsExternalInit`
- `LanguageCodeInfoJsonConverter`
- `MaritalStatus`
- `MimeType`
- `Month`
- `MonthFlags`
- `NamePrefix`
- `NameSuffix`
- `NullableLanguageCodeInfoJsonConverter`
- `Option`
- `PhoneType`
- `Race`
- `RegexPatterns`
- `Religion`
- `ResultBuilder`
- `ResultExtensions`
- `ResultLoggingExtensions`
- `ResultVoid`
- `Sex`
- `SocialPlatform`
- `SortDirection`
- `StringDateTimeConverter`
- `StringDecimalConverter`
- `StringDoubleConverter`
- `StringEnumConverter`
- `StringIntBoolConverter`
- `StringIntBoolNullableConverter`
- `StringIntConverter`
- `StringIntNullableConverter`
- `StringLongConverter`
- `StringLongNullableConverter`
- `StringValueAttribute`
- `TaskExtensions`
- `TaskResult`
- `Unit`
- `USState`
- `Utilities`
- `ValidationErrorCodes`
- `ValidationHelpers`
- `VeteranStatus`
- `YesNo`

<!-- LYO_README_SYNC:END -->

