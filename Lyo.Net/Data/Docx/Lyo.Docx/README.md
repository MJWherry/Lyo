# Lyo.Docx

Opens a .docx, returns paragraph and table text, and writes SmartFormat {Token} replacements back into the package. A token split across runs is joined before it is replaced.

## Features

- ReadText returns paragraph and table text.
- Fill replaces {Token} placeholders and leaves unknown tokens in place.

## Examples

### Fill a name token

```csharp
var docx = new DocxService(new FormatterService());
var filled = docx.Fill(bytes, new { Name = "Ada" });
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Formatter` (direct, lyo)
- `DocumentFormat.OpenXml` `3.1.1` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)