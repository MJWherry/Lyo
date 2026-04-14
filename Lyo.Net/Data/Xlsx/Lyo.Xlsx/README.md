# Lyo.Xlsx

A production-ready Excel (XLSX) processing library for .NET using ClosedXML and ExcelDataReader.

## Features

- ✅ **Export to Excel** - Export collections to XLSX files
- ✅ **Parse Excel Files** - Read XLSX files into dictionaries or typed objects
- ✅ **Multi-Sheet Support** - Export multiple data sets to separate worksheets
- ✅ **Property Selection** - Export only specific properties
- ✅ **CSV Conversion** - Convert XLSX files to CSV format
- ✅ **Async Support** - Full async/await support with cancellation tokens
- ✅ **Thread-Safe** - Safe for concurrent use
- ✅ **Error Handling** - Comprehensive error handling and validation
- ✅ **Logging** - Built-in logging support

## Quick Start

```csharp
using Lyo.Xlsx;

var service = new XlsxService();

// Export to file
var data = new[] { new Person { Name = "Alice", Age = 30 } };
service.ExportToXlsx(data, "output.xlsx");

// Parse from file
var dictionary = service.ParseXlsxFileAsDictionary("input.xlsx");

// Convert to CSV
service.ConvertXlsxToCsv("input.xlsx", "output.csv");

// Async operations with cancellation
var cts = new CancellationTokenSource();
await service.ExportToXlsxAsync(data, "output.xlsx", ct: cts.Token);
```

## Production Ready

This library has been reviewed for production use and includes:

- ✅ Thread-safe operations
- ✅ Comprehensive error handling
- ✅ Input validation
- ✅ Cancellation token support throughout
- ✅ Logging support
- ✅ Memory-efficient operations
- ✅ Detailed error messages

## Error Handling

The library provides detailed error messages:

- **Cancellation**: Operations respect cancellation tokens and provide clear cancellation messages
- **File Errors**: Detailed messages when files cannot be read or written
- **Data Errors**: Clear errors when data cannot be processed

## Thread Safety

The `XlsxService` class is thread-safe and can be used concurrently from multiple threads.

## Important Notes

### Async Operations

Async methods use `Task.Run` for CPU-bound operations (ClosedXML doesn't provide async APIs). Cancellation tokens are checked at key points to ensure responsive cancellation.

### Memory Usage

For very large Excel files, consider using streaming operations or processing in batches to manage memory usage.




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Xlsx.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `ClosedXML` | `[0.99,)` |
| `ExcelDataReader` | `[3.8,)` |
| `ExcelDataReader.DataSet` | `[3.8,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Xlsx.Models`

## Public API (generated)

Top-level `public` types in `*.cs` (*3*). Nested types and file-scoped namespaces may omit some entries.

- `Extensions`
- `XlsxErrorCodes`
- `XlsxService`

<!-- LYO_README_SYNC:END -->

## License

[Your License Here]

