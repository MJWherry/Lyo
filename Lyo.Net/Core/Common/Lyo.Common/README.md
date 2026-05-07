# Lyo.Common

Common primitives and helpers shared across the Lyo library suite: result/error models, validation that returns results, extension methods, utility functions, enum/record metadata
lookups, and JSON converters.

## Features

- **Ensure** - Guard-style validators that return `Result`/`Result<T>` instead of throwing (`NotNull`, `NotEmpty`, `InRange`, `That`, etc.).
- **Error and ErrorBuilder** - Structured error model with fluent construction APIs.
- **Result models** - `Result`, `Result<T>`, `ResultVoid`, and `BulkResult<T>` for success/failure workflows.
- **Result extensions** - Composition helpers such as combine, first-success selection, matching, and success/failure callbacks.
- **Utility helpers** - Common routines like hashing, file size conversions, and expression-based property path extraction.
- **CollectionExtensions** - Efficient collection/list/array helpers for null/empty checks and materialization/wrapping behavior.
- **Typed extension classes** - Explicit `*Extensions` classes for stream/string/scalar/dictionary/enum/file metadata/language lookup helpers.
- **JSON converters** - `System.Text.Json` converters for package-specific serialization scenarios.

## Quick Start

```csharp
using Lyo.Common;
using Lyo.Common.Builders;

// Build a structured error object
var error = ErrorBuilder.Create()
    .WithCode("VALIDATION_ERROR")
    .WithMessage("Invalid input")
    .Build();

// Result success/failure
var ok = Result<string>.Success("value");
var failed = Result<string>.Failure("Invalid request", "BAD_REQUEST");

// Convert exceptions to Error/Result
try {
    throw new InvalidOperationException("Bad state");
}
catch (Exception ex) {
    var asError = Error.FromException(ex);
    var asResult = Result<string>.Failure(asError);
}
```

```csharp
using Lyo.Common;

// Ensure returns Result instead of throwing
var notNull = Ensure.NotNull(input, nameof(input));
var inRange = Ensure.InRange(count, 1, 100, nameof(count));

if (!notNull.IsSuccess) {
    // work with error details from result
}
```

```csharp
using Lyo.Common;

// String and scalar extensions
var fallback = maybeNull.OrDefault("default");
var compactId = id.Truncated(start: 6, end: 28);
var parsed = "42".ToScalar<int>();

// Dictionary conversion helper
var payload = new Dictionary<string, object> { ["count"] = "3" };
var count = payload.GetValueAs<int>("count");
```

```csharp
using Lyo.Common;
using Lyo.Common.Enums;

// File and MIME helpers
var fileType = "report.pdf".GetFileTypeFromExtension();
var mimeType = "photo.jpg".GetMimeTypeFromExtension();
var mimeValue = mimeType.ToMimeString();

// Language and metadata lookups
var language = "en".FromISO639_1();
var statusInfo = 404.FromHttpStatusCode();
```

```csharp
using Lyo.Common;

// Collection helpers
IEnumerable<int> numbers = GetNumbers();
var list = numbers.AsListOrToList();
var readOnly = numbers.AsReadOnlyCollectionOrToList();
```

## Main Areas

- **`Ensure`**: Validation API for result-driven flows.
- **`Error` / `ErrorBuilder`**: Structured error payload construction and transport.
- **`Result*` types**: Standardized operation contracts for success/failure.
- **`CollectionExtensions`**: Materialization/wrapping helpers optimized to avoid unnecessary copies.
- **Extension classes**: Organized by responsibility:
    - stream and string helpers
    - scalar and dictionary conversion helpers
    - enum metadata helpers
    - file/geographic/language/status lookup helpers
- **`Utilities`**: Miscellaneous shared helper functions.
- **JSON converters**: Serializer support types used by Lyo packages.

## Dependencies

*(Synchronized from `Lyo.Common.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package            | Version |
|--------------------|---------|
| `System.Text.Json` | `[10,)` |

### Project references

- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)