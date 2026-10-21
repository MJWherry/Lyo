# Lyo.Api.Import

Validates an import request and dispatches to a format handler. JSON (array, wrapped rows path, NDJSON) is built in. YAML, XML, TOML, CSV, and XLSX are addon packages. A file over
the row or byte cap fails the document and returns no rows.

## Features

- Built-in JSON import that round-trips a file written by JSON export when the column map is empty.
- Partial success: a bad element is an issue and the other rows stay.
- Duplicate target properties fail the document before any row is read.

## Examples

### Registration

```csharp
builder.Services.AddLyoApiImport();
builder.Services.AddYamlImport();
builder.Services.AddXmlImport();
builder.Services.AddTomlImport();
builder.Services.AddCsvImport();
builder.Services.AddXlsxImport();
app.MapLyoImport();
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)