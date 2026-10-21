# Lyo.Json

The JSON contract Lyo APIs, clients, and message-queue envelopes serialize against lives in Lyo.Json: camelCase properties, case-insensitive reads, nulls omitted on write,
camelCase string enums, and ReferenceHandler.IgnoreCycles so EF-shaped graphs serialize.

JsonService.Default is that contract. Pass a JsonOptions instance or new JsonService (o => { ... }) to change naming, enums, number handling, write omission, encoding, and
comments. Pass a JsonSerializerOptions instance and it is copied and used as-is. One instance is safe for concurrent parse and export. Options are fixed after construction.

Byte methods take an optional Encoding. A missing argument uses the service encoding (UTF-8 without a BOM). A character span is already decoded. File and stream reads detect a BOM.
Async twins and NDJSON streaming exist on net10.0.

Converters under JsonConverters/ stay unregistered until UseStringScalarConverters is set. Add them for one integration through LyoJsonSerializerOptionsBuilder. Namespaces match
the package: options are Lyo.Json.LyoJsonSerializerOptions and converters live in Lyo.Json.JsonConverters.

## Features

- JsonService.Default, new JsonService (JsonOptions), new JsonService (configure), or new JsonService (JsonSerializerOptions) for camelCase, snake_case, kebab-case, or PascalCase,
  string or numeric enums, and write omission of nulls, defaults, empty strings, and empty collections.
- `Order` is Defined (declaration order), Alphabetical, or Specified. Specified uses `[MemberOrder]` and `OrderNames`. Defined does not sort.
- `ExportFormattedToJsonString` writes formatter fields in dictionary order. `ExportColumnsToJsonString` writes a header-to-property map in that same order. `Order` does not
  reorder either list.
- Optional Encoding on ParseBytes, Parse (ReadOnlySpan<byte>), ExportToJsonBytes, and TryExport. UTF-8 uses the System.Text.Json span APIs.
- NDJSON: ParseNdjsonFile, ParseNdjsonStream, ParseNdjsonString, and ParseNdjsonBytes, plus net10 IAsyncEnumerable for file and stream. Export writes one compact value per line.
- LyoJsonSerializerOptions.Create, Create (configure), and ApplyTo for the HTTP contract. LyoJsonSerializerOptionsBuilder adds converters.
- A top-level JSON array binds List<T> or T[].
- JsonNodeGraph converts a JsonNode to dictionaries, lists, and scalars so YAML, XML, and TOML share this contract.
- ObjectJsonConverter and the string scalar converters stay opt-in.

## Examples

### Parse and export a person

```csharp
using Lyo.Json;

var person = JsonService.Default.ParseString<Person>("""{"givenName":"Ada"}""");
var snake = new JsonService(o => o.Naming = DocumentNaming.SnakeCase);
string text = snake.ExportToJsonString(person);
```

### Serialize against the shared contract

```csharp
using Lyo.Json;

var options = LyoJsonSerializerOptions.Create();
var json = JsonSerializer.Serialize(dto, options);
var back = JsonSerializer.Deserialize<MyDto>(json, options);
```

## ASP.NET export

When an ASP.NET JsonOptions instance is registered, construct new JsonService (serializerOptions: aspNetSerializerOptions). The service copies those options and does not apply
JsonOptions on top, and it does not mutate the host instance. ConfigureHttpJsonOptions can copy the same defaults with LyoJsonSerializerOptions.ApplyTo.

## Why record converters live in metadata

SocialPlatformInfoJsonConverter and LanguageCodeInfoJsonConverter resolve a code to a registry record, so they stay in Lyo.Common.Metadata. A project that only wants the scalar
converters does not acquire the catalogs.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `System.Text.Encoding.CodePages` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)