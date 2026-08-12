# Lyo.Compression.Snappier

Snappy compression addon for `Lyo.Compression`. Registers a Snappier `ICompressorFactory`.

## Examples

### Register with DI

```csharp
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Compression.Snappier;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

services.AddSnappierCompressor();
services.AddCompressionService(options => options.DefaultAlgorithm = SnappierCompressionAlgorithm.Instance);
services.AddDefaultCompressionService<CompressionService>();

// Or from appsettings (CompressionOptions section)
services.AddSnappierCompressor();
services.AddCompressionServiceFromConfiguration(configuration, CompressionServiceOptions.SectionName);
services.AddDefaultCompressionService<CompressionService>();
```

## Benchmarks

- [Benchmark summary](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md)

## Dependency injection

See [`Lyo.Compression`](../Lyo.Compression/README.md) for the full DI, `ICompressionResolver`, and configuration guide. **File storage reads:** register `AddSnappierCompressor()`
when stored metadata may reference Snappy; decompression uses `ICompressionResolver`, not only the service default codec.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` — (direct, lyo)
- `EasyCompressor.Snappier` `2.1.0` — (direct, third-party)
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