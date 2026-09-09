# Lyo.Compression.BZip2

Addon that plugs BZip2 into `Lyo.Compression` by registering a BZip2 `ICompressorFactory`.

## Examples

### Wire it into DI

```csharp
using Lyo.Compression;
using Lyo.Compression.BZip2;
using Lyo.Compression.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

services.AddBZip2Compressor();
services.AddCompressionService(options => options.DefaultAlgorithm = BZip2CompressionAlgorithm.Instance);
services.AddDefaultCompressionService<CompressionService>();

// Or from IConfiguration
services.AddBZip2Compressor();
services.AddCompressionServiceFromConfiguration(configuration, CompressionServiceOptions.SectionName);
services.AddDefaultCompressionService<CompressionService>();
```

## Benchmarks

- [Benchmarks at a glance](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md)

## DI registration

Keyed services, ICompressionResolver, and CompressionOptions in appsettings.json are documented in [`Lyo.Compression`](../Lyo.Compression/README.md). When stored metadata might name BZip2, file-storage reads need AddBZip2Compressor(). On read, [ICompressionResolver](../Lyo.Compression/README.md#icompressionresolver-per-algorithm-dispatch) decompresses from that metadata.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` (direct, lyo)
- `SharpZipLib` `1.4.2` (direct, third-party)
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