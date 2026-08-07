# Lyo.Compression.XZ

XZ / LZMA2 compression addon for `Lyo.Compression`. Registers an XZ `ICompressorFactory`.

## Examples

### Register with DI

```csharp
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Compression.XZ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

services.AddXzCompressor();
services.AddCompressionService(options => options.DefaultAlgorithm = XzCompressionAlgorithm.Instance);
services.AddDefaultCompressionService<CompressionService>();

// Or from IConfiguration
services.AddXzCompressor();
services.AddCompressionServiceFromConfiguration(configuration, CompressionServiceOptions.SectionName);
services.AddDefaultCompressionService<CompressionService>();
```

## Benchmarks

- [Benchmark summary](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md)

## Dependency injection

See [`Lyo.Compression`](../Lyo.Compression/README.md) for the registration overview, `ICompressionResolver`, and configuration file example. **File storage reads:** register `AddXzCompressor()` when stored metadata may reference XZ; reads dispatch through `ICompressionResolver` by `CompressionAlgorithm` metadata.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` — (direct, lyo)
- `Joveler.Compression.XZ` `5.0.2` — (direct, third-party)
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