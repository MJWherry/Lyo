# Lyo.Compression

Compress and decompress bytes, strings, streams, and files through ICompressionService (default codec plus Resolver, AlgorithmSelector, and ResolveForCompress) and ICompressionResolver (explicit per-algorithm compress/decompress, implemented by CompressionService). Batch file work and atomic file writes are included.

With GenerateDocumentationFile set in Directory.Build.props, IntelliSense on ICompressionService and CompressionServiceOptions carries the same behavioral detail as the method summaries below.

## Features

- Algorithms from CompressionAlgorithm: GZip (default on netstandard2.0); Brotli (default on net10.0+; not on netstandard2.0); Deflate; ZLib (not on netstandard2.0); Snappier (Snappy); ZstdSharp (Zstandard); LZ4; LZMA; BZip2 (SharpZipLib); XZ (Joveler.Compression.XZ; native liblzma required on Linux: apt install liblzma5).
- Byte array, string (with encoding), stream (sync and async), file (sync and async), batch files with parallel work, and Base64.
- ICompressionResolver. Per-algorithm dispatch with cached factories. File storage uses it for metadata-driven decompress.
- Safe to register as a singleton. File writes go to a temp path then rename, so a failed compress leaves no partial target. MaxInputSize caps input and decompressed output. Unknown encodings fall back to UTF-8.
- File I/O uses 64 KB buffers by default. Batch file work honors MaxParallelFileOperations.

## Examples

### Basic compression

```csharp
using Lyo.Compression;

// Create compression service with default options
var service = new CompressionService();

// Compress data
var original = "Hello, World!"u8.ToArray();
var compressInfo = service.Compress(original, out var compressed);

// Decompress data
var decompressInfo = service.Decompress(compressed, out var decompressed);

// Verify round-trip
Console.WriteLine($"Original: {original.Length} bytes");
Console.WriteLine($"Compressed: {compressed.Length} bytes");
Console.WriteLine($"Compression ratio: {compressInfo.CompressionRatio:P2}");
Console.WriteLine($"Decompressed matches original: {original.SequenceEqual(decompressed)}");
```

### Custom configuration

```csharp
using Lyo.Compression;
using System.IO.Compression;

var options = new CompressionServiceOptions
{
    DefaultAlgorithm = CompressionAlgorithm.Brotli,
    DefaultCompressionLevel = CompressionLevel.Optimal,
    MaxInputSize = 100L * 1024 * 1024 * 1024, // 100 GB limit
    MaxParallelFileOperations = 8,
    DefaultEncoding = "utf-8"
};

var service = new CompressionService(options: options);
```

### Compress and decompress bytes

```csharp
var service = new CompressionService();
var data = Encoding.UTF8.GetBytes("This is a test string that will be compressed");

// Compress
var compressInfo = service.Compress(data, out var compressed);
Console.WriteLine($"Compressed {data.Length} bytes to {compressed.Length} bytes");
Console.WriteLine($"Compression ratio: {compressInfo.CompressionRatio:P2}");

// Decompress
var decompressInfo = service.Decompress(compressed, out var decompressed);
Console.WriteLine($"Decompressed {compressed.Length} bytes to {decompressed.Length} bytes");
Console.WriteLine($"Decompression time: {decompressInfo.DecompressionTimeMs}ms");

// Verify
Assert.Equal(data, decompressed);
```

### String compression

```csharp
var service = new CompressionService();

// Compress string (uses UTF-8 by default)
var text = "Hello, World! 你好世界!";
var compressInfo = service.CompressString(text, out var compressed);

// Decompress string
var decompressInfo = service.DecompressString(compressed, out var decompressed);
Assert.Equal(text, decompressed);

// With custom encoding
var compressInfoUtf16 = service.CompressString(text, out var compressedUtf16, Encoding.Unicode);
var decompressInfoUtf16 = service.DecompressString(compressedUtf16, out var decompressedUtf16, Encoding.Unicode);
Assert.Equal(text, decompressedUtf16);
```

### Stream compression

```csharp
var service = new CompressionService();
var original = Encoding.UTF8.GetBytes("Stream compression test");

// Synchronous stream compression
using var inputStream = new MemoryStream(original);
using var compressedStream = new MemoryStream();
service.Compress(inputStream, compressedStream);

// Synchronous stream decompression
compressedStream.Position = 0;
using var decompressedStream = new MemoryStream();
service.Decompress(compressedStream, decompressedStream);

Assert.Equal(original, decompressedStream.ToArray());

// Asynchronous stream compression
using var inputStreamAsync = new MemoryStream(original);
using var compressedStreamAsync = new MemoryStream();
await service.CompressAsync(inputStreamAsync, compressedStreamAsync);

compressedStreamAsync.Position = 0;
using var decompressedStreamAsync = new MemoryStream();
await service.DecompressAsync(compressedStreamAsync, decompressedStreamAsync);

Assert.Equal(original, decompressedStreamAsync.ToArray());
```

### File compression

```csharp
var service = new CompressionService();
var inputFile = "document.txt";
var outputFile = "document.txt" + service.FileExtension; // e.g., "document.txt.br"

// Synchronous file compression
var compressInfo = service.CompressFile(inputFile, outputFile);
Console.WriteLine($"Compressed file: {compressInfo.InputFilePath}");
Console.WriteLine($"Output file: {compressInfo.OutputFilePath}");
Console.WriteLine($"Compression ratio: {compressInfo.CompressionRatio:P2}");

// Synchronous file decompression
var decompressInfo = service.DecompressFile(outputFile);
Console.WriteLine($"Decompressed file: {decompressInfo.OutputFilePath}");

// Asynchronous file compression
var compressInfoAsync = await service.CompressFileAsync(inputFile, outputFile);

// Asynchronous file decompression
var decompressInfoAsync = await service.DecompressFileAsync(outputFile);
```

### Batch operations

```csharp
var service = new CompressionService();

// Batch compression of byte arrays
var items = new Dictionary<string, byte[]>
{
    { "item1", Encoding.UTF8.GetBytes("First item") },
    { "item2", Encoding.UTF8.GetBytes("Second item") },
    { "item3", Encoding.UTF8.GetBytes("Third item") }
};

var compressed = service.Compress(items);
var decompressed = service.Decompress(compressed);

foreach (var key in items.Keys)
{
    Assert.Equal(items[key], decompressed[key]);
}

// Batch file compression
var files = new List<string>
{
    "file1.txt",
    "file2.txt",
    "file3.txt"
};

var compressResult = service.CompressFiles(files);
Console.WriteLine($"Total files: {compressResult.TotalFiles}");
Console.WriteLine($"Successful: {compressResult.SuccessfulFilesCount}");
Console.WriteLine($"Failed: {compressResult.FailedFilesCount}");
Console.WriteLine($"Average compression ratio: {compressResult.AverageCompressionRatio:P2}");

// Process failed files
foreach (var failed in compressResult.FailedFiles)
{
    Console.WriteLine($"Failed: {failed.FilePath} - {failed.ErrorMessage}");
}

// Asynchronous batch file compression with parallel processing
var compressResultAsync = await service.CompressFilesAsync(files);
```

### Base64 compression

```csharp
var service = new CompressionService();
var data = Encoding.UTF8.GetBytes("Data to compress and encode");

// Compress and encode to base64
var compressInfo = service.CompressToBase64(data, out var base64String);
Console.WriteLine($"Base64 string length: {base64String.Length}");

// Decode from base64 and decompress
var decompressInfo = service.DecompressFromBase64(base64String, out var decompressed);
Assert.Equal(data, decompressed);
```

### Try methods

```csharp
var service = new CompressionService();

// TryCompress - returns false on failure instead of throwing
if (service.TryCompress(data, out var compressed, out var info))
{
    Console.WriteLine($"Compression successful: {info.CompressionRatio:P2}");
}
else
{
    Console.WriteLine("Compression failed");
}

// TryDecompress - returns false on failure instead of throwing
if (service.TryDecompress(compressed, out var decompressed, out var decompressInfo))
{
    Console.WriteLine($"Decompression successful");
}
else
{
    Console.WriteLine("Decompression failed - data may be corrupted");
}
```

### Register with DI

```csharp
using Lyo.Compression;
using Lyo.Compression.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// 1) Defaults — concrete + built-in factories (GZip, Deflate, Brotli, ZLib on net10+)
services.AddCompressionService();
services.AddDefaultCompressionService<CompressionService>();

// 2) Configure via lambda (same pattern as other Lyo libraries)
services.AddCompressionService(options =>
{
    options.DefaultAlgorithm = CompressionAlgorithm.Brotli;
    options.DefaultCompressionLevel = CompressionLevel.Optimal;
    options.MaxInputSize = 10L * 1024 * 1024 * 1024;
    options.MaxParallelFileOperations = 8;
});
services.AddDefaultCompressionService<CompressionService>();

// 3) Bind from IConfiguration / appsettings.json
services.AddCompressionServiceFromConfiguration(
    configuration,
    configSectionName: CompressionServiceOptions.SectionName); // "CompressionOptions"
services.AddDefaultCompressionService<CompressionService>();
```

### Keyed registration

```csharp
services.AddCompressionServiceKeyed("tenant-a", options =>
    options.DefaultAlgorithm = CompressionAlgorithm.GZip);
services.AddCompressionServiceKeyed("tenant-b", options =>
    options.DefaultAlgorithm = CompressionAlgorithm.Deflate);

// Resolve: GetRequiredKeyedService<ICompressionService>("tenant-a")
```

### Addon factories

```csharp
services.AddLz4Compressor(); // Lyo.Compression.Lz4
services.AddZstdCompressor(); // Lyo.Compression.Zstd
// Then set DefaultAlgorithm on options (lambda, config file, or keyed configure).
```

### Inject ICompressionService

```csharp
public class MyController(ICompressionService compressionService)
{
    public IActionResult CompressData(byte[] data)
    {
        var info = compressionService.Compress(data, out var compressed);
        return Ok(new { compressed, ratio = info.CompressionRatio });
    }
}
```

### CompressionServiceOptions

```csharp
public class CompressionServiceOptions
{
    // Shared defaults when no options instance is supplied (CompressionService ctor, policy selector fallback)
    public static CompressionServiceOptions Default { get; }

    // Compression algorithm (default: Brotli for net10+, GZip for .NET Standard 2.0)
    public CompressionAlgorithm DefaultAlgorithm { get; set; }
    
    // Compression level (default: Optimal)
    public CompressionLevel DefaultCompressionLevel { get; set; }
    
    // Maximum parallel file operations (default: Environment.ProcessorCount)
    public int MaxParallelFileOperations { get; set; }
    
    // Default encoding for string operations (default: "utf-8")
    public string DefaultEncoding { get; set; }
    
    // Buffer sizes for file I/O (default: 65536 bytes / 64 KB)
    public int DefaultFileBufferSize { get; set; }
    public int AsyncFileBufferSize { get; set; }
    
    // Maximum input size in bytes (default: 10 GB)
    // Prevents DoS attacks from extremely large inputs
    public long MaxInputSize { get; set; }
}
```

### appsettings.json

```json
{
  "CompressionOptions": {
    "DefaultAlgorithm": "Brotli",
    "DefaultCompressionLevel": "Optimal",
    "MaxParallelFileOperations": 8,
    "DefaultEncoding": "utf-8",
    "DefaultFileBufferSize": 65536,
    "AsyncFileBufferSize": 65536,
    "MaxInputSize": 10737418240
  }
}
```

### ICompressionResolver

```csharp
services.AddCompressionService();
services.AddDefaultCompressionService<CompressionService>();

// Resolve either contract from the same instance
var service = provider.GetRequiredService<ICompressionService>();
var resolver = provider.GetRequiredService<ICompressionResolver>();
```

### ICompressionAlgorithmSelector

```csharp
services.AddCompressionServiceFromConfiguration(configuration, "CompressionOptions");
services.AddCompressionPolicySelector(configuration, "CompressionOptions:Policy");
```

### Policy in appsettings

```json
{
  "CompressionOptions": {
    "DefaultAlgorithm": "Brotli",
    "Policy": {
      "MinCompressSizeBytes": 4096,
      "BuiltInDefaultsEnabled": true,
      "DefaultAlgorithm": "Brotli",
      "Rules": [
        { "Categories": ["Compressed", "Images", "Audio"], "Compress": false },
        {
          "Tenants": ["acme"],
          "ContentTypePrefixes": ["application/json"],
          "MinSizeBytes": 65536,
          "Algorithm": "LZ4"
        }
      ]
    }
  }
}
```

### Encoding fallback

```csharp
var service = new CompressionService(options: new CompressionServiceOptions 
{ 
    DefaultEncoding = "InvalidEncodingName" 
});

// Will not throw - falls back to UTF-8
var text = "Hello, World!";
var compressInfo = service.CompressString(text, out var compressed);
var decompressInfo = service.DecompressString(compressed, out var decompressed);
Assert.Equal(text, decompressed); // Works correctly with UTF-8 fallback
```

### Parallel file operations

```csharp
var options = new CompressionServiceOptions
{
    MaxParallelFileOperations = 16 // Process 16 files concurrently
};

var service = new CompressionService(options: options);
var result = await service.CompressFilesAsync(files); // Processes files in parallel
```

## Benchmarks

Zstd compresses 100 MB in tens of milliseconds at multi-GB/s throughput.

- Portfolio suite: `compression`
- [Zstd compress](/benchmarks/compression)
- [Benchmark summary](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md)

## Atomic file writes

File operations write to a unique temporary file first, then rename. If compression fails, no partial file is left at the target path.

## Register with DI

What gets registered

| Call | Registers in DI |
| -------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| AddCompressionService() | Built-in ICompressorFactory, CompressionServiceOptions, CompressionService, ICompressionResolver pointing at the same instance. |
| AddCompressionResolver() | ICompressionResolver only (idempotent; usually unnecessary. Already called by AddCompressionService). |
| AddDefaultCompressionService<CompressionService>() | ICompressionService pointing at the same instance as CompressionService. |
| AddCompressionPolicySelector(…) | ICompressionAlgorithmSelector + CompressionPolicyOptions (optional write-time policy for file storage). |
| AddLz4Compressor() / other addons | Additional ICompressorFactory entries only. |
| AddCompressionServiceKeyed("key", …) | Per-key options + CompressionService + ICompressionService (no separate default mapper). |

AddCompressionService does not register ICompressionService until you call AddDefaultCompressionService<TConcrete>(). ICompressionResolver is registered automatically.

Unkeyed registration is the typical app path. See the appsettings.json example for the CompressionOptions section shape.

Keyed registration is for multi-tenant or multiple policies.

Optional addon factories: register before or with AddCompressionService. Idempotent.

## ICompressionResolver

- DI: AddCompressionService() registers ICompressionResolver pointing at the same CompressionService singleton. Call AddCompressionResolver() only if you registered CompressionService yourself and still need the interface mapping.
- Defaults: new CompressionService() / new CompressionService(options: null) uses CompressionServiceOptions.Default (static singleton; do not mutate).

## ICompressionAlgorithmSelector

Register policy-driven algorithm selection for file storage saves, and any caller that injects the selector. Rules use FileTypeInfo category, MIME/content-type, size, tenant, and environment name. First matching rule wins, then built-in heuristics, then environment profile, then default. File storage read path: decompress uses metadata.CompressionAlgorithm via ICompressionService.Resolver, not the configured default. Optional overrides: GetFileAsync(id, compressionAlgorithmOverride: …, ct) or FileStorageServiceBaseOptions.DecompressionAlgorithmOverride. Register factories for every algorithm you may read from historical files.

## Validation

- `MaxParallelFileOperations` must be >= 1
- `DefaultFileBufferSize` must be >= 1024 bytes
- `AsyncFileBufferSize` must be >= 1024 bytes
- `MaxInputSize` must be >= 1024 bytes

## Input size limits

MaxInputSize rejects oversized inputs:

```csharp
var options = new CompressionServiceOptions
{
    MaxInputSize = 100L * 1024 * 1024 * 1024 // 100 GB limit
};

var service = new CompressionService(options: options);

// This will throw ArgumentOutsideRangeException if data exceeds MaxInputSize
var largeData = new byte[options.MaxInputSize + 1];
service.Compress(largeData, out _); // Throws exception
```

## Decompression bomb protection

Both compressed input size and decompressed output size are checked against MaxInputSize:

```csharp
// Both compressed input size AND decompressed output size are validated
var service = new CompressionService(options: new CompressionServiceOptions 
{ 
    MaxInputSize = 10L * 1024 * 1024 * 1024 // 10 GB limit
});

// If a 1MB compressed file decompresses to 11GB, this will throw InvalidOperationException
try
{
    service.Decompress(compressedData, out var decompressed);
}
catch (InvalidOperationException ex)
{
    // "Decompressed size (11811160064 bytes) exceeds maximum allowed input size (10737418240 bytes)"
}
```

CompressionService validates both:

- **Compressed input size.** Rejects extremely large compressed files.
- **Decompressed output size.** Rejects a small compressed payload that expands past MaxInputSize.

## Atomic file operations

File operations write to a unique temporary file first, then atomically rename it:

```csharp
// If compression fails, no partial file is left at the target location
try
{
    service.CompressFile("input.txt", "output.txt.br");
}
catch (Exception ex)
{
    // output.txt.br does not exist if compression failed
    // Temporary file (GUID-based .tmp) is automatically cleaned up
}
```

Temporary files use GUID-based naming to avoid colliding with existing files. The temporary file is created in the same directory as the target file and is cleaned up on failure.

## Path validation

File paths are validated and canonicalized to block directory traversal:

```csharp
// These will throw ArgumentException:
service.CompressFile("../../../etc/passwd", "output.br"); // Directory traversal
service.CompressFile("file\0name.txt", "output.br"); // Invalid characters
```

## Service properties

- `string FileExtension { get; }`. Extension associated with this instance's algorithm (`Constants.Data.AlgorithmExtensions`), e.g. `.gz`, `.br`, `.zst`.
- `CompressionAlgorithm Algorithm { get; }`. Algorithm bound to this service instance at construction time.

## Core methods

- `CompressionInfo Compress(byte[] bytes, out byte[] compressed)`. Compress byte array.
- `DecompressionInfo Decompress(byte[] compressedBytes, out byte[] decompressed)`. Decompress byte array.
- `CompressionInfo CompressString(string text, out byte[] compressed, Encoding? encoding = null)`. Compress string.
- `DecompressionInfo DecompressString(byte[] compressedBytes, out string decompressed, Encoding? encoding = null)`. Decompress string.
- `CompressionInfo CompressToBase64(byte[] bytes, out string base64String)`. Compress byte array to Base64 string.
- `DecompressionInfo DecompressFromBase64(string base64String, out byte[] decompressed)`. Decompress from Base64 string.

## Compression algorithms

```csharp
public enum CompressionAlgorithm
{
    Brotli, // not available on netstandard2.0
    BZip2,
    Deflate,
    GZip,
    LZ4,
    LZMA,
    Snappier,
    XZ,
    ZLib, // not available on netstandard2.0
    ZstdSharp
}
```

File extensions are sourced from `Lyo.Common.Records.FileTypeInfo` via `Constants.Data.AlgorithmExtensions` (e.g. `.gz`, `.br`, `.zst`, `.lz4`, `.lzma`, `.bz2`, `.xz`, `.snappy`, `.deflate`, `.zlib`).

## Information types

- `CompressionInfo`. Compression statistics (`CompressionRatio`, `SpaceSavedPercent`, `TimeMs`).
- `DecompressionInfo`. Decompression statistics (`ExpansionRatio`, `SizeIncreasePercent`, `DecompressionTimeMs`).
- `FileCompressionInfo`. File compression statistics.
- `FileDecompressionInfo`. File decompression statistics.
- `BatchFileCompressionResult`. Batch compression results.
- `BatchFileDecompressionResult`. Batch decompression results.

## Performance

BenchmarkDotNet suite: [`Lyo.Compression.Benchmarks`](../Lyo.Compression.Benchmarks/). Write-up in [`BENCHMARK_SUMMARY.md`](../Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md) (last run June 14, 2026, .NET 10.0.9, Linux Mint 22.1, Intel Core Ultra 7 155U). Payloads use random bytes unless noted. Real compressible data improves ratios and lowers BZip2 allocation.

## Benchmark numbers (June 2026)

| Workload | Fastest compress | Fastest decompress | GZip baseline |
| ----------------------- | ------------------ | ------------------ | ----------------- |
| 1 KB in-memory | Snappier (~878 ns) | LZ4 (~271 ns) | ~17 µs compress |
| 1 MB in-memory | **LZ4 (~117 µs)** | **Zstd (~70 µs)** | ~19 ms / ~381 µs |
| 10 MB in-memory | **LZ4 (~1.8 ms)** | **Zstd (~1.3 ms)** | ~197 ms / ~9.0 ms |
| 100 MB in-memory | **LZ4 (~18 ms)** | **Zstd (~13 ms)** | ~2.0 s / ~78 ms |
| 100 MB streaming | **Zstd (~65 ms)** | Zstd (~58 ms) | ~1.9 s / ~65 ms |
| 1 GB streaming compress | **Zstd (~1.0 s)** | n/a | ~19.9 s |

Zstd vs GZip streaming compress: about 29× at 100 MB, about 20× at 1 to 2 GB. Policy defaults (FastAlgorithm: LZ4, ArchivalAlgorithm: Zstd, default Brotli) align with these numbers.

> **BZip2 on random data.** SharpZipLib allocates a sort stack on every internal QSort3 call. Benchmarks show about 764 MB alloc per 1 MB compress on noise, but about 8 MB on compressible text. Use BZip2 for `.tar.bz2` interop on compressible payloads, not pre-compressed blobs.

## Algorithm selection

- **LZ4.** Fastest compress at 1 MB+ in benchmarks. Best for real-time, streaming, and policy FastAlgorithm.
- **Snappier.** Fastest at 1 KB. Very low latency, lower ratio.
- **ZstdSharp.** Best large-file decompress. Strong streaming compress. Policy ArchivalAlgorithm.
- **Brotli** (default for net10.0+). Strong ratio. Slower than LZ4/Zstd but faster than GZip on many sizes.
- **GZip.** Compatibility baseline. Similar to Deflate/ZLib.
- **Deflate / ZLib.** Similar to GZip.
- **LZMA / XZ.** High ratio, slow compress. Archival (XZ needs native `liblzma` on Linux: `apt install liblzma5`).
- **BZip2.** `.tar.bz2` interop via SharpZipLib. Avoid on incompressible or pre-compressed data (see note above).

## Buffer sizes

Default buffer sizes (64 KB) balance memory and throughput. For high-throughput hosts, increase them:

```csharp
var options = new CompressionServiceOptions
{
    DefaultFileBufferSize = 131072, // 128 KB
    AsyncFileBufferSize = 131072 // 128 KB
};
```

## Concurrency

CompressionService is safe to register as a singleton:

```csharp
// Safe to use concurrently
services.AddSingleton<ICompressionService, CompressionService>();

// Multiple threads can use the same instance
var service = serviceProvider.GetRequiredService<ICompressionService>();

// Thread 1
Task.Run(() => service.Compress(data1, out _));

// Thread 2
Task.Run(() => service.Compress(data2, out _));

// Both operations are safe and independent
```

CompressionServiceOptions is mutable, but options are validated and used only during service construction. Once the service is created, it treats options as read-only.

## File extensions

The service adds the file extension for the algorithm:

```csharp
var service = new CompressionService(options: new CompressionServiceOptions 
{ 
    DefaultAlgorithm = CompressionAlgorithm.Brotli 
});

var outputFile = service.CompressFile("document.txt");
// outputFile.OutputFilePath will be "document.txt.br"
```

## Stream position

Stream operations reset the input stream position to 0 if the stream supports seeking:

```csharp
using var stream = new MemoryStream(data);
stream.Position = 100; // Position is not at start

service.Compress(stream, outputStream); // Automatically resets to position 0
```

## Cancellation tokens

All async methods accept cancellation tokens, including batch operations:

```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(5));

try
{
    // Single file operation
    await service.CompressFileAsync("large-file.txt", ct: cts.Token);
    
    // Batch operations check cancellation between file processing iterations
    var files = new[] { "file1.txt", "file2.txt", "file3.txt" };
    await service.CompressFilesAsync(files, ct: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Compression was cancelled");
}
```

Batch operations check the cancellation token before each file, so a long batch can stop between files.

## Error handling

- `ArgumentNullException`. Null input parameters.
- `ArgumentException`. Invalid arguments (empty data, invalid paths, etc.).
- `ArgumentOutsideRangeException`. Input exceeds `MaxInputSize` or invalid options.
- `FileNotFoundException`. Input file does not exist.
- `InvalidOperationException`. File size exceeds limits, or decompressed size exceeds `MaxInputSize` (decompression bomb protection).
- `OperationCanceledException`. Operation was cancelled.

## See also

- [`BENCHMARK_SUMMARY.md`](../Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md). BenchmarkDotNet results (algorithms, streaming, last run June 2026).
- [`EasyCompressor`](https://www.nuget.org/packages/EasyCompressor). Compressor abstraction backing GZip/Brotli/Deflate/ZLib/Snappier/Zstd/LZ4/LZMA paths.
- [`Joveler.Compression.XZ`](https://www.nuget.org/packages/Joveler.Compression.XZ). XZ (LZMA2) implementation (requires native `liblzma` on Linux).
- [`SharpZipLib`](https://www.nuget.org/packages/SharpZipLib). Used for BZip2.
- [.NET Compression Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.io.compression)

## Public types

| Type | Description |
| ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ICompressionService / CompressionService | Default-codec contract and implementation. FileExtension, Algorithm, byte/string/stream/file/batch/base64 methods (sync + async + Try*). |
| ICompressionResolver | Per-algorithm compress/decompress (GetCompressor, stream/byte APIs with explicit CompressionAlgorithm). Implemented by CompressionService. |
| ICompressionAlgorithmSelector / CompressionPolicyAlgorithmSelector | Optional write-time policy (rules, env profiles, built-in skips). Used by file storage on save when registered. |
| CompressionServiceOptions | Default (static), DefaultAlgorithm, DefaultCompressionLevel, MaxInputSize, MaxParallelFileOperations, buffer sizes, EnableMetrics. SectionName = "CompressionOptions". |
| CompressionPolicyOptions | Policy rules, MinCompressSizeBytes, environment profiles. Bind from CompressionOptions:Policy. |
| CompressionAlgorithm | Enum: GZip, Brotli*, Deflate, ZLib*, Snappier, ZstdSharp, LZ4, LZMA, BZip2, XZ. (*Brotli/ZLib require net10.0; unavailable on netstandard2.0.) |
| CompressionInfo / DecompressionInfo | In-memory operation metadata. |
| FileCompressionInfo / FileDecompressionInfo | File-level operation metadata (input/output paths, sizes, timings). |
| BatchFileCompressionResult / BatchFileDecompressionResult | Batch metadata + per-file failures (FailedFiles). |
| CompressionFileInfo / DecompressionFileInfo / FileCompressionInfo / FileDecompressionInfo / FailedFileOperation / BatchCompressionResult / BatchDecompressionResult / CompressionProgress | Supporting models in Lyo.Compression.Models. |
| Extensions | DI: AddCompressionService(), AddCompressionResolver(), AddDefaultCompressionService<TConcrete>(), AddCompressionServiceFromConfiguration, AddCompressionPolicySelector, AddCompressionServiceKeyed. |
| CompressionErrorCodes | Stable error code strings. |

The Compressors/ folder (BZip2Compressor, XZCompressor) contains internal helpers backing those two algorithms. Callers should go through ICompressionService.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Streams` (direct, lyo)
- `EasyCompressor` `2.1.0` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)