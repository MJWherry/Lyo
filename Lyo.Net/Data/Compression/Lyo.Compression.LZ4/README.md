# Lyo.Compression.LZ4

LZ4 compression addon for `Lyo.Compression`. Registers an `LZ4` `ICompressorFactory` backed by `EasyCompressor.LZ4`.

## Dependency injection

```csharp
using Lyo.Compression;
using Lyo.Compression.LZ4;
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

`AddLz4Compressor()` only registers a factory. Pair with [`Lyo.Compression`](../Lyo.Compression/README.md) for `AddCompressionService`,
`AddDefaultCompressionService<CompressionService>()`, and keyed registration.
