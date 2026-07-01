# Lyo.Compression.Zstd

Zstandard compression addon for `Lyo.Compression`. Registers a Zstd `ICompressorFactory`.

## Dependency injection

```csharp
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Compression.Zstd;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

services.AddZstdCompressor();

services.AddCompressionService(options =>
{
    options.DefaultAlgorithm = ZstdCompressionAlgorithm.Instance;
});
services.AddDefaultCompressionService<CompressionService>();

// Or from configuration (CompressionOptions section in appsettings.json)
services.AddZstdCompressor();
services.AddCompressionServiceFromConfiguration(configuration, CompressionServiceOptions.SectionName);
services.AddDefaultCompressionService<CompressionService>();
```

See [`Lyo.Compression`](../Lyo.Compression/README.md) for the full registration tree, keyed services, `ICompressionResolver`, and `CompressionServiceOptions` binding.

**File storage reads:** register `AddZstdCompressor()` when historical metadata may reference Zstd; decompression uses `ICompressionResolver`, not only
`ICompressionService.DefaultAlgorithm`.
