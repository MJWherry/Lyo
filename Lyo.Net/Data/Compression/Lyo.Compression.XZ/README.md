# Lyo.Compression.XZ

XZ / LZMA2 compression addon for `Lyo.Compression`. Registers an XZ `ICompressorFactory`.

## Dependency injection

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

See [`Lyo.Compression`](../Lyo.Compression/README.md) for the registration overview and configuration file example.
