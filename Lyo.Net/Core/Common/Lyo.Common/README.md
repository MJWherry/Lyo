# Lyo.Common

Shared primitives: ID generators, file/MIME/language/HTTP/file-size metadata, geometry, secure RNG, typed extensions, and shared `System.Text.Json` options.

> **Note.** Earlier versions of this README also described `Ensure`, `Error`, `ErrorBuilder`, and `Result*` types. Those live in [`Lyo.Result`](../../Result/Lyo.Result/README.md), not here. `Lyo.Common` has no dependency on results.

## Features

- **ID generators** (`Identifiers/`). `Ksuid`, `LyoGuid`, `NanoId`, `Snowflake`, `Ulid`, and `AutoIncrementIdGenerator` for thread-safe, sortable identifiers.
- **Record metadata catalogs** (`Records/`). `FileTypeInfo` (`.GetFileTypeFromExtension`, MIME mapping, two-key envelope suffix, common storage-resolution suffix list), `FileSizeUnitInfo`, `HttpStatusCodeInfo`, `PortInfo` (well-known ports + `PortCategory`, `FromPort`/`FromName`/`ByCategory`, implicit `int`), `LanguageCodeInfo`, `ProgrammingLanguageInfo`, `BoundingBox2D`.
- **Enum catalogs** (`Enums/`). `FileTypeFlags`, `MimeType`, `PortCategory`, language and HTTP enums with metadata-attribute lookups.
- **Typed extension classes** (`Extensions/`). `StringExtensions` (truncate, ellipsis, case helpers), `ScalarExtensions` (`ToScalar<T>`, parsing helpers), `DictionaryExtensions` (`GetValueAs<T>`), `StreamExtensions` (bounded reads, copy helpers), `EnumMetadataExtensions`, `LanguageExtensions`, `TypeInfoExtensions`.
- **`CollectionExtensions`.** Materialization helpers (`AsListOrToList`, `AsReadOnlyCollectionOrToList`) that skip a copy when the source is already the right shape.
- **`Utilities`.** Shared helpers: `SafeDispose`, file-size conversions, expression-based property-path extraction.
- **Pathing** (`Pathing/`). `PathStyle` (`Host` / `Posix`) and `PathHelpers` for combine, full-path normalize (`.`/`..`), file/dir name, `SanitizeFileName`, and under-root jail checks with Uri-style throw helpers (`ThrowIfEscapesRoot`, `ThrowIfInvalidPath`).
- **Cryptographic random** (`Security/CryptographicRandom`). `RandomNumberGenerator`-backed byte / int / string helpers. Other Lyo packages use this instead of `System.Random` for security-adjacent work.
- **`Disposable`.** Base class and lambda disposable.
- **`HashCodeHelpers`.** `HashCode.Combine`-style helpers for `netstandard2.0`.
- **`LyoJsonSerializerOptions`.** Shared `JsonSerializerOptions` (case-insensitive, ignore-null, enum-as-string) plus converters in `JsonConverters/` that other packages reuse.

## Examples

### Quick start

```csharp
using Lyo.Common.Identifiers;
using Lyo.Common.Records;
using Lyo.Common.Extensions;
using Lyo.Common.Enums;

// IDs
string ksuid = Ksuid.NewId(); // sortable 27-char base62
string nano = NanoId.New(size: 21); // url-safe random
string ulid = Ulid.NewUlid();
Guid guid = LyoGuid.NewSequential();

// File metadata
FileTypeInfo? type = "report.pdf".GetFileTypeFromExtension();
MimeType? mime = "photo.jpg".GetMimeTypeFromExtension();
string? mimeString = mime?.ToMimeString();

// Strings / scalars
string truncated = id.Truncated(start: 6, end: 28);
int parsed = "42".ToScalar<int>();

// JSON
var options = LyoJsonSerializerOptions.Default;
```

### Conversion

```csharp
using Lyo.Common.Conversion;

// Throwing conversions (TypeConversionException on failure)
int i = TypeConversion.ConvertTo<int>("42");
Guid id = TypeConversion.ConvertTo<Guid>("0d64a685-2fa2-4c48-b06c-b9b0ac9f6a2e");
object? any = TypeConversion.ConvertTo(value, targetType);
int[] ints = (int[])TypeConversion.ConvertToWithCollections(jsonArray, typeof(int[]))!;

// Non-throwing variants
if (TypeConversion.TryConvertTo<DateTime>(value, out var when)) { /* ... */ }
int port = TypeConversion.ConvertToOrDefault("8080", defaultValue: 80);

// Strings that look like JSON ({ or [) deserialize into complex targets as a last resort
var payload = TypeConversion.ConvertTo<MyDto>("""{"Name":"abc","Count":3}""");

// Spans (span-native parsing on net10, ToString fallback on netstandard2.0)
long ticks = TypeConversion.ConvertTo<long>("637500000000000000".AsSpan());

// Booleans — lenient tokens: true/t/1/y/yes/on and false/f/0/n/no/off (case-insensitive)
bool on = TypeConversion.ToBoolean("yes");
bool ok = TypeConversion.TryToBoolean("enabled", out var b, trueValues: ["enabled"], falseValues: ["disabled"]);

// JsonElement (strict typed accessors; ConvertTo handles lenient token coercion)
object? loose = TypeConversion.FromJsonElement(element); // string/long/double/bool/null/list
bool got = TypeConversion.TryFromJsonElement<int>(element, out var n);

// Enums / sequences
var status = TypeConversion.EnumOrDefault("Active", StatusEnum.Unknown);
var maybe = TypeConversion.EnumOrNull<StatusEnum>(raw);
var values = TypeConversion.ToEnumerable(scalarOrArrayOrJson); // always a sequence
int[] bulk = TypeConversion.ConvertToArray<int>(values)!;
```

### Conversion

```csharp
TypeConversion.Logger = loggerFactory.CreateLogger("TypeConversion");
```

### Pathing

```csharp
using Lyo.Common.Pathing;

var root = "/mem/lyo";
var child = PathHelpers.Combine(PathStyle.Posix, root, "session", "file.txt");
var full = PathHelpers.GetFullPath(PathStyle.Posix, child);
PathHelpers.ThrowIfEscapesRoot(PathStyle.Posix, root, full);
```

## Identifier matrix

| Generator | Sortable | Length | Notes |
| -------------------------- | ------------- | --------------------------- | ------------------------------------------------------- |
| `Ksuid` | (time-prefix) | 27 chars (base62) | Drop-in monotonic-ish id; good URL safety. |
| `LyoGuid` | (sequential) | 36 chars (GUID) | UUID v7-style for DB index locality. |
| `NanoId` | | configurable (default 21) | URL-safe random; collision rates documented per length. |
| `Snowflake` | | int64 | Configurable worker + datacenter ids. |
| `Ulid` | | 26 chars (Crockford base32) | ULID spec. |
| `AutoIncrementIdGenerator` | | int64 | Pure in-process counter for tests / fixtures. |

## Records / metadata

`FileTypeInfo` is the canonical registry for Lyo file types: human-readable name, canonical extensions (e.g. `.ag`, `.chacha`, `.ag2k`), two-key envelope suffix ( `TwoKeyEnvelopeSuffix = "2k"`), and `CommonStorageResolutionSuffixes` (used by `Lyo.FileStorage` to resolve persisted blobs when explicit metadata isn't present).

## JSON

`LyoJsonSerializerOptions` is the shared default `JsonSerializerOptions`. The converters under `JsonConverters/` (e.g. enum-as-string fallbacks, raw JSON pass-through) are pre-registered so other Lyo packages can reuse them without duplicating wiring.

## Conversion

`Lyo.Common.Conversion.TypeConversion` converts CLR objects, strings, character spans, and `JsonElement` values to target types. Callers include API patch binding, query filters, web-component grids, and message-queue
envelopes. It unwraps nullables, parses enums (name or numeric), `Guid`/date/time, and materializes collections (`T[]`, `List<T>`, `HashSet<T>`, `IReadOnlyList<T>`, `ISet<T>`, any concrete collection with an `IEnumerable<T>` constructor).

Failures throw `TypeConversionException` (derives `InvalidOperationException`) carrying `Value`, `SourceType`, and `TargetType`. Assign the static logger to get
Debug on success, Warning on `Try*` misses, and Error before throws. Logging is zero-allocation via `LoggerMessage.Define` and off by default (`NullLogger`).

`TypeConversionExtensions` adds the reflection helpers the engine uses: `IsNumericType()`, `IsNullable()`, `IsCollectionType()`, `GetCollectionElementType()`,
`GetFriendlyTypeName()`, `IsObjectEnumerable()`, and `TryGetAsEnumerable<T>()`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `System.Memory` `4.6.3` (direct, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)