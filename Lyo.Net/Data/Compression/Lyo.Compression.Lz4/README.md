# Lyo.Compression.Lz4

LZ4 compression addon for `Lyo.Compression`. Registers an `LZ4` `ICompressorFactory` backed by `EasyCompressor.LZ4`.

## Examples

### Register with DI

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

- [Benchmark summary](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md)

## Dependency injection

`AddLz4Compressor()` only registers a factory. Pair with [`Lyo.Compression`](../Lyo.Compression/README.md) for `AddCompressionService`,
`AddDefaultCompressionService<CompressionService>()`, and keyed registration. **File storage reads:** register this factory if any stored files may have `CompressionAlgorithm` =
LZ4; [ `ICompressionResolver`](../Lyo.Compression/README.md#icompressionresolver-per-algorithm-dispatch) dispatches by metadata on `GetFileAsync` / `GetFileStreamAsync`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` — (direct, lyo)
- `EasyCompressor.LZ4` `2.1.0` — (direct, third-party)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)