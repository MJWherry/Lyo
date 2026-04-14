# Lyo.Csv

A production-ready CSV processing library for .NET using CsvHelper for reading and writing CSV files.

## Features

- ✅ **Clean API** - Simple, intuitive methods for reading and writing CSV files
- ✅ **Multiple Output Formats** - Export to file, stream, string, or byte array
- ✅ **Async Support** - Full async/await support for .NET 10.0+ with cancellation token support
- ✅ **Custom Class Maps** - Support for custom CSV mapping configurations
- ✅ **Property Selection** - Export only specific properties from your objects
- ✅ **Encoding Support** - Configurable encoding including UTF-8, UTF-16, UTF-32, and more
- ✅ **Error Handling** - Comprehensive input validation and error handling
- ✅ **Logging** - Built-in logging support via Microsoft.Extensions.Logging
- ✅ **Dependency Injection** - Full support for .NET dependency injection
- ✅ **Thread-Safe** - Safe for concurrent use from multiple threads
- ✅ **Stream Position Handling** - Automatically resets stream positions for reliable parsing

## Quick Start

### 1. Register Services

```csharp
using Lyo.Csv;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Simple registration
services.AddCsvService();

// With custom configuration
services.AddCsvService(config =>
{
    config.Delimiter = ";";
    config.HasHeaderRecord = true;
});

// With configuration builder
services.AddCsvService(() => new CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = ",",
    HasHeaderRecord = true
});
```

### 2. Use the Service

```csharp
public class MyService
{
    private readonly ICsvService _csvService;
    
    public MyService(ICsvService csvService)
    {
        _csvService = csvService;
    }
    
    public void ExportData()
    {
        var data = new[]
        {
            new Person { Id = 1, Name = "Alice", Age = 30 },
            new Person { Id = 2, Name = "Bob", Age = 25 }
        };
        
        // Export to file
        _csvService.ExportToCsv(data, "output.csv");
        
        // Export to string
        var csvString = _csvService.ExportToCsvString(data);
        
        // Export to byte array
        var csvBytes = _csvService.ExportToCsvBytes(data);
    }
    
    public void ImportData()
    {
        // Parse from file
        var records = _csvService.ParseFile<Person>("input.csv");
        
        foreach (var person in records)
        {
            Console.WriteLine($"{person.Name} - {person.Age}");
        }
    }
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
```

## Usage Examples

### Exporting Data

#### Export to File

```csharp
var data = new List<Person>
{
    new Person { Id = 1, Name = "Alice", Age = 30 },
    new Person { Id = 2, Name = "Bob", Age = 25 }
};

_csvService.ExportToCsv(data, "output.csv");
```

#### Export to Stream

```csharp
using var stream = new MemoryStream();
_csvService.ExportToCsvStream(data, stream);

// Stream is now ready to use
stream.Position = 0;
// ... use stream
```

#### Export to String

```csharp
var csvString = _csvService.ExportToCsvString(data);
Console.WriteLine(csvString);
// Output: Id,Name,Age
//         1,Alice,30
//         2,Bob,25
```

#### Export to Byte Array

```csharp
var csvBytes = _csvService.ExportToCsvBytes(data);
File.WriteAllBytes("output.csv", csvBytes);
```

### Exporting Selected Properties

You can export only specific properties from your objects:

```csharp
var data = new List<Person>
{
    new Person { Id = 1, Name = "Alice", Age = 30, Email = "alice@example.com" }
};

var selectedProperties = new List<PropertyInfo>
{
    typeof(Person).GetProperty(nameof(Person.Name))!,
    typeof(Person).GetProperty(nameof(Person.Email))!
};

_csvService.ExportToCsv(data, selectedProperties, "output.csv");
// Output: Name,Email
//         Alice,alice@example.com
```

### Parsing CSV Files

#### Parse to Typed Objects

```csharp
var records = _csvService.ParseFile<Person>("input.csv");

foreach (var person in records)
{
    Console.WriteLine($"{person.Name} - {person.Age}");
}
```

#### Parse to Dictionary

```csharp
var dictionary = _csvService.ParseFileAsDictionary("input.csv");

// dictionary[0] contains header row
// dictionary[1] contains first data row
// dictionary[2] contains second data row, etc.

foreach (var row in dictionary)
{
    Console.WriteLine($"Row {row.Key}:");
    foreach (var column in row.Value)
    {
        Console.WriteLine($"  Column {column.Key}: {column.Value}");
    }
}
```

### Async Operations

All export and parse operations have async versions for .NET 10.0+:

```csharp
// Async export
await _csvService.ExportToCsvAsync(data, "output.csv", ct);

// Async parse
var records = await _csvService.ParseFileAsync<Person>("input.csv", ct);

// Async export to bytes
var csvBytes = await _csvService.ExportToCsvBytesAsync(data, ct);
```

### Custom Class Maps

You can register custom class maps for advanced CSV mapping:

```csharp
// Define a custom map
public class PersonMap : ClassMap<Person>
{
    public PersonMap()
    {
        Map(m => m.Name).Name("Full Name");
        Map(m => m.Age).Name("Years Old");
        Map(m => m.Id).Ignore();
    }
}

// Register the map
_csvService.RegisterClassMap<PersonMap>();

// Now parsing will use the custom map
var records = _csvService.ParseFile<Person>("input.csv");
```

### Encoding Configuration

You can configure the encoding used for reading and writing CSV files:

```csharp
// Set UTF-8 encoding
_csvService.SetEncoding(Encoding.UTF8);

// Set UTF-16 encoding
_csvService.SetEncoding(Encoding.Unicode);

// Set UTF-8 with BOM
_csvService.SetEncoding(new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

// Export will use the configured encoding
_csvService.ExportToCsv(data, "output.csv");
```

### Custom CSV Configuration

You can set a custom CsvConfiguration:

```csharp
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = ";",
    HasHeaderRecord = true,
    IgnoreBlankLines = true,
    TrimOptions = TrimOptions.Trim
};

_csvService.SetCsvConfiguration(config);
```

### Dependency Injection Examples

#### Simple Registration

```csharp
services.AddCsvService();
```

#### With Configuration

```csharp
services.AddCsvService(config =>
{
    config.Delimiter = ";";
    config.HasHeaderRecord = true;
    config.IgnoreBlankLines = true;
});
```

#### With Configuration Builder

```csharp
services.AddCsvService(() => new CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = ",",
    HasHeaderRecord = true
});
```

#### With Service Provider Access

```csharp
services.AddCsvService((provider, config) =>
{
    var logger = provider.GetService<ILogger<CsvService>>();
    config.Delimiter = ",";
    // Configure based on other services if needed
});
```

## Advanced Usage

### Working with Streams

When working with streams, the service automatically handles stream positioning:

```csharp
// Stream position is automatically reset if needed
using var stream = new MemoryStream();
// ... write some data to stream
stream.Position = 100; // Stream is at position 100

// ParseStream will reset to position 0 automatically
var records = _csvService.ParseStream<Person>(stream);
```

### Error Handling

The service includes comprehensive input validation:

```csharp
try
{
    // These will throw ArgumentNullException if null
    _csvService.ExportToCsv(null, "file.csv");
    _csvService.ParseFile<Person>(null);
    
    // This will throw ArgumentException if empty
    _csvService.ExportToCsv(data, "");
    
    // This will throw FileNotFoundException if file doesn't exist
    _csvService.ParseFile<Person>("nonexistent.csv");
}
catch (ArgumentNullException ex)
{
    // Handle null argument
}
catch (ArgumentException ex)
{
    // Handle invalid argument
}
catch (FileNotFoundException ex)
{
    // Handle missing file
}
```

### Logging

The service logs operations at debug level:

```csharp
// Configure logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Logs will include:
// - Export operations with type and path
// - Parse operations with type and path
// - Bad data warnings when encountered
```

## API Reference

### Export Methods

- `ExportToCsv<T>(IEnumerable<T> data, string csvFilePath)` - Export to file
- `ExportToCsvStream<T>(IEnumerable<T> data, Stream csvStream)` - Export to stream
- `ExportToCsv<T>(IEnumerable<T> data, TextWriter writer)` - Export to TextWriter
- `ExportToCsvString<T>(IEnumerable<T> data)` - Export to string
- `ExportToCsvBytes<T>(IEnumerable<T> data)` - Export to byte array
- `ExportToCsv<T>(IEnumerable<T> data, IList<PropertyInfo> selectedProperties, string csvFilePath)` - Export selected
  properties to file
- `ExportToCsvStream<T>(IEnumerable<T> data, IList<PropertyInfo> selectedProperties, Stream csvStream)` - Export
  selected properties to stream
- `ExportToCsv<T>(IEnumerable<T> data, IList<PropertyInfo> selectedProperties, TextWriter writer)` - Export selected
  properties to TextWriter
- `ExportToCsvString<T>(IEnumerable<T> data, IList<PropertyInfo> selectedProperties)` - Export selected properties to
  string
- `ExportToCsvBytes<T>(IEnumerable<T> data, IList<PropertyInfo> selectedProperties)` - Export selected properties to
  byte array

### Async Export Methods (NET 10.0+)

- `ExportToCsvAsync<T>(IEnumerable<T> data, string csvFilePath, CancellationToken ct = default)`
- `ExportToCsvStreamAsync<T>(IEnumerable<T> data, Stream csvStream, CancellationToken ct = default)`
- `ExportToCsvAsync<T>(IEnumerable<T> data, TextWriter writer, CancellationToken ct = default)`
- `ExportToCsvStringAsync<T>(IEnumerable<T> data, CancellationToken ct = default)`
- `ExportToCsvBytesAsync<T>(IEnumerable<T> data, CancellationToken ct = default)`
-

`ExportToCsvAsync<T>(IEnumerable<T> data, IList<PropertyInfo> selectedProperties, string csvFilePath, CancellationToken ct = default)`
-
`ExportToCsvStreamAsync<T>(IEnumerable<T> data, IList<PropertyInfo> selectedProperties, Stream csvStream, CancellationToken ct = default)`
-
`ExportToCsvAsync<T>(IEnumerable<T> data, IList<PropertyInfo> selectedProperties, TextWriter writer, CancellationToken ct = default)`
-
`ExportToCsvStringAsync<T>(IEnumerable<T> data, IList<PropertyInfo> selectedProperties, CancellationToken ct = default)`
-
`ExportToCsvBytesAsync<T>(IEnumerable<T> data, IList<PropertyInfo> selectedProperties, CancellationToken ct = default)`

### Parse Methods

- `ParseFile<T>(string csvFilePath)` - Parse file to typed objects
- `ParseStream<T>(Stream csvStream)` - Parse stream to typed objects
- `ParseFileAsDictionary(string csvFilePath)` - Parse file to dictionary structure
- `ParseStreamAsDictionary(Stream csvStream)` - Parse stream to dictionary structure

### Async Parse Methods (NET 10.0+)

- `ParseFileAsync<T>(string csvFilePath, CancellationToken ct = default)` - Parse file to typed objects
- `ParseStreamAsync<T>(Stream csvStream, CancellationToken ct = default)` - Parse stream to typed objects
- `ParseFileAsDictionaryAsync(string csvFilePath, CancellationToken ct = default)` - Parse file to
  dictionary structure
- `ParseStreamAsDictionaryAsync(Stream csvStream, CancellationToken ct = default)` - Parse stream to
  dictionary structure

### Configuration Methods

- `RegisterClassMap<TMap>()` - Register a custom class map
- `SetCsvConfiguration(CsvConfiguration csvConfiguration)` - Set custom CSV configuration
- `SetEncoding(Encoding encoding)` - Set encoding for reading/writing

## Thread Safety

The `CsvService` class is thread-safe and can be used concurrently from multiple threads. Each operation is independent
and maintains no shared mutable state between method calls.




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Csv.csproj`.)*

**Target framework:** `net10.0;netstandard2.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `CsvHelper` | `[33.1,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `System.Text.Encoding.CodePages` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.Csv.Models`
- `Lyo.Exceptions`

## Public API (generated)

Top-level `public` types in `*.cs` (*8*). Nested types and file-scoped namespaces may omit some entries.

- `CsvErrorCodes`
- `CsvService`
- `DecimalCsvConverter`
- `Extensions`
- `Int32CsvConverter`
- `Int64CsvConverter`
- `IsExternalInit`
- `YesNoBoolCsvConverter`

<!-- LYO_README_SYNC:END -->

## License

Copyright © Lyo

