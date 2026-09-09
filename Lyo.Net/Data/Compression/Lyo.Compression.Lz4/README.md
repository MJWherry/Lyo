# Lyo.Compression.Lz4

`Lyo.Compression` addon that registers an `LZ4` `ICompressorFactory` using `EasyCompressor.LZ4`.

## Examples

### Add the factory in DI

```csharp
using Lyo.Compression;
using Lyo.Compression.Lz4;
using Lyo.Compression.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

services.AddLz4Compressor();

// Configure via lambda
services.AddCompressionService(options =>
{
    options.DefaultAlgorithm = Lz4CompressionAlgorithm.Instance;
    options.DefaultCompressionLevel = CompressionLevel.Fastest;
});
services.AddDefaultCompressionService<CompressionService>();

// Or bind from appsettings (CompressionOptions section)
services.AddLz4Compressor();
services.AddCompressionServiceFromConfiguration(configuration, CompressionServiceOptions.SectionName);
services.AddDefaultCompressionService<CompressionService>();
```

## Benchmarks

- [Benchmarks at a glance](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md)

## DI registration

AddLz4Compressor() registers the factory and nothing else. AddCompressionService, AddDefaultCompressionService<CompressionService>(), and keyed registration live in [`Lyo.Compression`](../Lyo.Compression/README.md). If stored files might carry CompressionAlgorithm = LZ4, register this factory so file-storage reads can open them. [ICompressionResolver](../Lyo.Compression/README.md#icompressionresolver-per-algorithm-dispatch) picks the codec from metadata in GetFileAsync / GetFileStreamAsync.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` (direct, lyo)
- `EasyCompressor.LZ4` `2.1.0` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)