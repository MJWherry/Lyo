# Lyo.Compression.Lzma

LZMA compression addon for `Lyo.Compression`. Registers an LZMA `ICompressorFactory`.

## Examples

### Register with DI

```csharp
using Lyo.Compression;
using Lyo.Compression.Lzma;
using Lyo.Compression.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

services.AddLzmaCompressor();
services.AddCompressionService(options => options.DefaultAlgorithm = LzmaCompressionAlgorithm.Instance);
services.AddDefaultCompressionService<CompressionService>();

// Or bind options from IConfiguration
services.AddLzmaCompressor();
services.AddCompressionServiceFromConfiguration(configuration, CompressionServiceOptions.SectionName);
services.AddDefaultCompressionService<CompressionService>();
```

## Benchmarks

- [Benchmark summary](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md)

## Dependency injection

See [`Lyo.Compression`](../Lyo.Compression/README.md) for keyed registration, ICompressionResolver, and appsettings examples. File storage reads: register AddLzmaCompressor() when stored metadata may reference LZMA. [ICompressionResolver](../Lyo.Compression/README.md#icompressionresolver-per-algorithm-dispatch) handles read-time decompress.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` (direct, lyo)
- `EasyCompressor.LZMA` `2.1.0` (direct, third-party)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)