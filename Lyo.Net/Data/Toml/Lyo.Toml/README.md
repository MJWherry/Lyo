# Lyo.Toml

TomlService.Default writes camelCase keys, omits nulls, and exports a root array as [[items]]. new TomlService (o => { ... }) changes the items key, nested table style, date style,
naming, numbers, and encoding. IgnoreNulls cannot be false, because TOML has no null. One instance is safe for concurrent parse and export. Options are fixed after construction.

Byte methods take an optional Encoding. A missing argument uses the service encoding (UTF-8 without a BOM). A character span is already decoded. File and stream reads detect a BOM.
Async twins exist on net10.0.

## Features

- TomlService.Default and new TomlService (configure) for camelCase, snake_case, kebab-case, or PascalCase, and an ItemsKey other than items.
- `Order` follows the shared document options: Defined, Alphabetical, or Specified through `[MemberOrder]` and `OrderNames`.
- `ExportFormattedToTomlString` writes formatter fields in dictionary order. `ExportColumnsToTomlString` writes a header-to-property map in that same order. `Order` does not
  reorder either list.
- Optional Encoding on ParseBytes, Parse (ReadOnlySpan<byte>), ExportToTomlBytes, and TryExport.
- NestedTables.Inline writes address = { city = "London" }. Arrays of objects stay [[items]]. DateStyle selects offset, local, or local date.
- ConvertToJson and ConvertFromJson for a file, stream, bytes, and a string. Cross-format conversion is two calls through JSON.
- Write omission of defaults, empty strings, and empty collections follows the shared document options. Nulls are always omitted.

## Examples

### Round-trip through TOML

```csharp
using Lyo.Toml;

string doc = TomlService.Default.ExportToTomlString(person);
string json = TomlService.Default.ConvertToJson(doc);
var people = new TomlService(o => o.ItemsKey = "people");
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `System.Text.Encoding.CodePages` `10.0.5` (direct, microsoft)
- `Tomlyn` `2.10.1` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)