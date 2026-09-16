# Lyo.Yaml

YamlService.Default is the Lyo document contract: camelCase, omitted nulls, and a block document. new YamlService(o => { ... }) changes naming, enums, number handling, write omission, quoting, multi-document mode, and encoding. One instance is safe for concurrent parse and export. The serializer and deserializer are built once and reused. Options are fixed after construction.

Byte methods take an optional Encoding. A missing argument uses the service encoding (UTF-8 without a BOM). A character span is already decoded. File and stream reads detect a BOM. ConvertToJson stays compact so it matches the JSON contract. Async twins exist on net10.0.

## Features

- YamlService.Default and new YamlService(configure) for camelCase, snake_case, kebab-case, or PascalCase, block or flow style, explicit --- markers, and multi-document lists.
- `Order` follows the shared document options: Defined, Alphabetical, or Specified through `[MemberOrder]` and `OrderNames`.
- `ExportFormattedToYamlString` writes formatter fields in dictionary order. `ExportColumnsToYamlString` writes a header-to-property map in that same order. `Order` does not reorder either list.
- Optional Encoding on ParseBytes, Parse(ReadOnlySpan<byte>), ExportToYamlBytes, and TryExport.
- QuoteStrings selects plain, single-quoted, or double-quoted strings. AllowAliases expands anchors. The default rejects aliases.
- ConvertToJson and ConvertFromJson for a file, stream, bytes, and a string. Cross-format conversion is two calls through JSON.
- Write omission of nulls, defaults, empty strings, and empty collections follows the shared document options.

## Examples

### Round-trip through YAML

```csharp
using Lyo.Yaml;

string doc = YamlService.Default.ExportToYamlString(person);
string json = YamlService.Default.ConvertToJson(doc);
var flow = new YamlService(o => o.WriteIndented = false);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `System.Text.Encoding.CodePages` `10.0.5` (direct, microsoft)
- `YamlDotNet` `18.1.0` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)