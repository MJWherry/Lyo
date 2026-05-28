# Lyo.Common

Cross-cutting primitives shared across the Lyo library suite: ID generators, file/MIME/language/HTTP/file-size metadata, geometry, secure RNG, typed extension classes, and shared
`System.Text.Json` options.

> **Note** — earlier versions of this README also described `Ensure`, `Error`, `ErrorBuilder`, and `Result*` types. Those live in *
*[`Lyo.Result`](../../Result/Lyo.Result/README.md)**, not here. `Lyo.Common` has **no** dependency on results — it sits below them and provides primitives that the rest of the
> framework composes.

## Features

- **ID generators** (`Identifiers/`) — `Ksuid`, `LyoGuid`, `NanoId`, `Snowflake`, `Ulid`, and `AutoIncrementIdGenerator` for thread-safe, sortable identifiers.
- **Record metadata catalogs** (`Records/`) — `FileTypeInfo` (`.GetFileTypeFromExtension`, MIME mapping, two-key envelope suffix, common storage-resolution suffix list),
  `FileSizeUnitInfo`, `HttpStatusCodeInfo`, `LanguageCodeInfo`, `ProgrammingLanguageInfo`, `BoundingBox2D`.
- **Enum catalogs** (`Enums/`) — `FileTypeFlags`, `MimeType`, language and HTTP enums with metadata-attribute lookups.
- **Typed extension classes** (`Extensions/`) — `StringExtensions` (truncate, ellipsis, case helpers), `ScalarExtensions` (`ToScalar<T>`, parsing helpers), `DictionaryExtensions` (
  `GetValueAs<T>`), `StreamExtensions` (bounded reads, copy helpers), `EnumMetadataExtensions`, `LanguageExtensions`, `TypeInfoExtensions`.
- **`CollectionExtensions`** — materialization helpers (`AsListOrToList`, `AsReadOnlyCollectionOrToList`) that avoid redundant copies when the source is already the right shape.
- **`Utilities`** — small shared helpers (`SafeDispose`, file-size conversions, expression-based property-path extraction).
- **Cryptographic random** (`Security/CryptographicRandom`) — `RandomNumberGenerator`-backed byte / int / string helpers, used by other Lyo packages instead of `System.Random` for
  anything security-adjacent.
- **`Disposable`** — convenience base / lambda disposable.
- **`HashCodeHelpers`** — `HashCode.Combine`-style helpers for `netstandard2.0`.
- **`LyoJsonSerializerOptions`** — shared `JsonSerializerOptions` (case-insensitive, ignore-null, enum-as-string) plus converters in `JsonConverters/` that other packages reuse.

## Quick Start

```csharp
using Lyo.Common.Identifiers;
using Lyo.Common.Records;
using Lyo.Common.Extensions;
using Lyo.Common.Enums;

// IDs
string ksuid = Ksuid.NewId();          // sortable 27-char base62
string nano  = NanoId.New(size: 21);   // url-safe random
string ulid  = Ulid.NewUlid();
Guid   guid  = LyoGuid.NewSequential();

// File metadata
FileTypeInfo? type = "report.pdf".GetFileTypeFromExtension();
MimeType?     mime = "photo.jpg".GetMimeTypeFromExtension();
string?       mimeString = mime?.ToMimeString();

// Strings / scalars
string truncated = id.Truncated(start: 6, end: 28);
int    parsed    = "42".ToScalar<int>();

// JSON
var options = LyoJsonSerializerOptions.Default;
```

## Identifier matrix

| Generator                  | Sortable        | Length                      | Notes                                                   |
|----------------------------|-----------------|-----------------------------|---------------------------------------------------------|
| `Ksuid`                    | ✅ (time-prefix) | 27 chars (base62)           | Drop-in monotonic-ish id; good URL safety.              |
| `LyoGuid`                  | ✅ (sequential)  | 36 chars (GUID)             | UUID v7-style for DB index locality.                    |
| `NanoId`                   | ❌               | configurable (default 21)   | URL-safe random; collision rates documented per length. |
| `Snowflake`                | ✅               | int64                       | Configurable worker + datacenter ids.                   |
| `Ulid`                     | ✅               | 26 chars (Crockford base32) | ULID spec.                                              |
| `AutoIncrementIdGenerator` | ✅               | int64                       | Pure in-process counter for tests / fixtures.           |

## Records / metadata

`FileTypeInfo` is the canonical registry for Lyo file types: human-readable name, canonical extensions (e.g. `.ag`, `.chacha`, `.ag2k`), two-key envelope suffix (
`TwoKeyEnvelopeSuffix = "2k"`), and `CommonStorageResolutionSuffixes` (used by `Lyo.FileStorage` to resolve persisted blobs when explicit metadata isn't present).

## JSON

`LyoJsonSerializerOptions` is the shared default `JsonSerializerOptions`. The converters under `JsonConverters/` (e.g. enum-as-string fallbacks, raw JSON pass-through) are
pre-registered so other Lyo packages can reuse them without duplicating wiring.

## Dependencies

*(Synchronized from `Lyo.Common.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package            | Version | Notes                 |
|--------------------|---------|-----------------------|
| `System.Text.Json` | `[10,)` | *netstandard2.0 only* |

### Project references

- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)
