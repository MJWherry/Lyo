# Lyo.Csv

CsvHelper-backed implementation of [`Lyo.Csv.Models`](../Lyo.Csv.Models/README.md).
`CsvService` composes a `CsvWriter` and `CsvReader` to read and write CSV from
files, streams, byte arrays, URLs, and `TextWriter`/`TextReader`. Multi-targets
`net10.0;netstandard2.0`; async, streaming, and option-based overloads are only
available on `net10.0`.

## Features

- Strongly-typed read/write via `IEnumerable<T>` / `List<T>`.
- Row/column dictionary (`IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>`) read and write.
- `Lyo.DataTable.Models.DataTable` round-trip (`ParseFileAsDataTable`, `ExportToCsvFromDataTable`, plus an HTML helper `ExportToHtmlTable`).
- CSV → DataTable pooling via `CsvOptions.Pooling` / `CsvParseOptions.Pooling` (defaults `PoolValues=false`; estimate is `cols × (rows+1)` after the full CSV is buffered).
- Selected-property export (`IReadOnlyList<PropertyInfo>`), custom-header export (`IReadOnlyDictionary<string, PropertyInfo>`), and formatter export (`IReadOnlyDictionary<string, Func<T, string>>`).
- URL download helpers (`ParseFromUrl*`) that share an optional injected `HttpClient`.
- Append, combine, and split file operations (async, `net10.0` only).
- Streaming reads (`IAsyncEnumerable<T>`), chunked processing, statistics, schema validation, column-mapping parses, and CSV-to-CSV comparison.
- Custom `ClassMap` registration and `CsvConfiguration` overrides.
- Encoding configuration via `SetEncoding`; `CodePagesEncodingProvider` is registered in the service constructor so legacy encodings work out of the box.
- Bundled `ITypeConverter` implementations: `DecimalCsvConverter`, `Int32CsvConverter`, `Int64CsvConverter`, `YesNoBoolCsvConverter`.
- `CsvErrorCodes` constants (`CSV_EXPORT_FAILED`, `CSV_PARSE_FAILED`, `CSV_OPERATION_CANCELLED`, `CSV_FILE_OPERATION_FAILED`, `CSV_VALIDATION_FAILED`) used when wrapping failures in `Result<T>`.

## Examples

### Register with DI

```csharp
using Lyo.Csv;

services.AddCsvService();
services.AddCsvService(o => {
    o.Pooling.PoolValues = true; // opt in for high-duplication grids
    o.Pooling.PoolingCellThreshold = 512;
});

services.AddCsvService(config => {
    config.Delimiter = ";";
    config.HasHeaderRecord = true;
    config.IgnoreBlankLines = true;
});

services.AddCsvService(() => new CsvConfiguration(CultureInfo.InvariantCulture) {
    Delimiter = ",",
    HasHeaderRecord = true,
});

services.AddCsvService((provider, config) => {
    var feature = provider.GetRequiredService<IFeatureFlags>();
    config.Delimiter = feature.UseSemicolons ? ";" : ",";
});
```

### Quick start

```csharp
public sealed class ReportingService(ICsvService csv)
{
    public async Task ExportAsync(IEnumerable<Person> rows, Stream output, CancellationToken ct)
        => await csv.ExportToCsvStreamAsync(rows, output, ct);

    public async Task<List<Person>> ImportAsync(string path, CancellationToken ct)
        => await csv.ParseFileAsync<Person>(path, ct);
}

public sealed record Person(int Id, string Name, int Age);
```

### Custom configuration and class maps

```csharp
csv.SetEncoding(new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
csv.SetCsvConfiguration(new CsvConfiguration(CultureInfo.InvariantCulture) {
    Delimiter = ";",
    TrimOptions = TrimOptions.Trim,
    IgnoreBlankLines = true,
});

public sealed class PersonMap : ClassMap<Person>
{
    public PersonMap()
    {
        Map(p => p.Id).Name("Id").Ignore();
        Map(p => p.Name).Name("Full Name");
        Map(p => p.Age).Name("Years Old");
    }
}

csv.RegisterClassMap<PersonMap>();
```

### Column shaping

```csharp
IReadOnlyList<PropertyInfo> selected = [
    typeof(Person).GetProperty(nameof(Person.Name))!,
    typeof(Person).GetProperty(nameof(Person.Age))!,
];
csv.ExportToCsv(rows, selected, "out.csv");

IReadOnlyDictionary<string, PropertyInfo> namedColumns = new Dictionary<string, PropertyInfo> {
    ["Full Name"] = typeof(Person).GetProperty(nameof(Person.Name))!,
    ["Years"] = typeof(Person).GetProperty(nameof(Person.Age))!,
};
await csv.ExportToCsvStreamAsync(rows, namedColumns, stream, ct);

IReadOnlyDictionary<string, Func<Person, string>> formatters = new Dictionary<string, Func<Person, string>> {
    ["Display"] = p => $"{p.Name} ({p.Age})",
    ["Id"] = p => p.Id.ToString("D6"),
};
await csv.ExportToCsvStreamAsync(rows, formatters, stream, ct);
```

### DataTable, dictionary, and HTML helpers

```csharp
Result<DataTable> parsed = csv.ParseFileAsDataTable("in.csv", hasHeaderRow: true);
csv.ExportToCsvFromDataTable(parsed.ValueOrThrow(), "out.csv");

var grid = csv.ParseFileAsDictionary("in.csv");
csv.ExportToCsvFromDictionary(grid, "out.csv");

string html = csv.ExportToHtmlTable(File.ReadAllBytes("in.csv"));
```

### Streaming, options, statistics, validation, comparison

```csharp
await foreach (var row in csv.ParseFileStreamingAsync<Person>("big.csv", new CsvParseOptions {
    ContinueOnError = true,
    OnError = err => log.LogWarning("Row {Row}: {Message}", err.RowNumber, err.Message),
    RowFilter = cells => cells["Status"] == "active",
    MaxRows = 100_000,
}, ct)) {
    // process row
}

CsvStatistics stats = await csv.GetStatisticsAsync("big.csv", ct);

await csv.ProcessFileInChunksAsync<Person>(
    "big.csv",
    chunkSize: 1_000,
    processChunk: async batch => await sink.WriteAsync(batch),
    ct: ct);

ValidationResult validation = await csv.ValidateAsync("in.csv", new CsvSchema {
    RequireAllColumns = true,
    AllowExtraColumns = false,
    Columns = [
        new CsvColumn { Name = "Id", IsRequired = true },
        new CsvColumn { Name = "Name", IsRequired = true },
    ],
}, ct);

CsvComparisonResult diff = await csv.CompareFilesAsync("v1.csv", "v2.csv", keyColumn: "Id", ct);
```

### Append / combine / split (net10.0)

```csharp
await csv.AppendToCsvAsync(rows, "log.csv", includeHeaderIfMissing: true, ct);
await csv.CombineCsvFilesAsync(parts, "merged.csv", includeHeaders: true, ct);
await csv.SplitCsvFileAsync("merged.csv", rowsPerFile: 10_000, outputDirectory: "chunks", ct);
```

## Benchmarks

UTF-8 export of 100,000 sample rows in tens of milliseconds.

- Portfolio suite: `csv`
- [CSV UTF-8 export](/benchmarks/csv)

## Dependency injection

`AddCsvService` registers a singleton `CsvService` and routes `ICsvService`, `ICsvWriter`, and `ICsvReader` to the same instance. Overloads accept `Action<CsvOptions>`, an options instance, `AddCsvServiceFromConfiguration` (binds `Csv` and optional `DataTablePooling` sections), or the existing `CsvConfiguration` / builder overloads (default CSV pooling remains off).

## Output targets

Each export path is available as file / stream / `TextWriter` / string / byte array
overloads, in sync (all TFMs) and async (`net10.0`) flavors:

```csharp
csv.ExportToCsv(rows, "out.csv");
csv.ExportToCsvStream(rows, stream);
csv.ExportToCsv(rows, textWriter);
string text = csv.ExportToCsvString(rows);
byte[] bytes = csv.ExportToCsvBytes(rows);

await csv.ExportToCsvAsync(rows, "out.csv", ct);
await csv.ExportToCsvStreamAsync(rows, stream, ct);
string textAsync = await csv.ExportToCsvStringAsync(rows, ct);
byte[] bytesAsync = await csv.ExportToCsvBytesAsync(rows, ct);
```

## URL and batch

```csharp
Result<DataTable> table = await csv.ParseFromUrlAsDataTableAsync(url, hasHeaderRow: true, ct);
List<Person> rows = await csv.ParseFromUrlAsync<Person>(url, ct);

IReadOnlyList<Result<DataTable>> results =
    await csv.BatchParseFilesAsDataTableAsync(paths, hasHeaderRow: true, ct);
```

If you do not supply an `HttpClient` to the constructor, a fresh one is created per URL
call and disposed afterward; production callers should inject one via DI.

## Custom configuration and class maps

The default `CsvConfiguration` used by `CsvService` enables `IgnoreBlankLines`, trims fields, normalizes header matching, disables constructor-parameter mapping, and logs `BadDataFound` warnings via `ILogger<CsvService>`. <!-- LYO_README_SYNC:BEGIN -->

## Public API (generated)

- `CsvErrorCodes`
- `CsvService`
- `DecimalCsvConverter`
- `Extensions`
- `Int32CsvConverter`
- `Int64CsvConverter`
- `IsExternalInit`
- `YesNoBoolCsvConverter`

## License

Copyright © Lyo

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Csv.Models` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `CsvHelper` `33.1.0` — (direct, third-party)
- `Microsoft.Extensions.Configuration` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (direct, microsoft)
- `System.Text.Encoding.CodePages` `10.0.5` — (direct, microsoft)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)