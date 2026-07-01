# Lyo.Compression.LZMA

LZMA compression addon for `Lyo.Compression`. Registers an LZMA `ICompressorFactory`.

## Dependency injection

```csharp
using Lyo.Compression;
using Lyo.Compression.LZMA;
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

See [`Lyo.Compression`](../Lyo.Compression/README.md) for keyed registration, `ICompressionResolver`, and appsettings examples.

**File storage reads:** register `AddLzmaCompressor()` when stored metadata may reference LZMA; [
`ICompressionResolver`](../Lyo.Compression/README.md#icompressionresolver-per-algorithm-dispatch) handles read-time decompress.
