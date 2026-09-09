# Lyo.Compression.Xz

XZ / LZMA2 support for `Lyo.Compression` through an XZ `ICompressorFactory`.

## Examples

### Register the XZ factory

```csharp
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Compression.Xz;
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

- [Benchmarks at a glance](Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks/BENCHMARK_SUMMARY.md)

## DI registration

Registration, ICompressionResolver, and the configuration-file sample are in [`Lyo.Compression`](../Lyo.Compression/README.md). When stored metadata might reference XZ, register AddXzCompressor() so file-storage reads work. ICompressionResolver then dispatches those reads from CompressionAlgorithm metadata.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` (direct, lyo)
- `Joveler.Compression.XZ` `5.0.2` (direct, third-party)
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