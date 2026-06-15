# Lyo.Compression.BZip2

BZip2 compression addon for `Lyo.Compression`. Registers a BZip2 `ICompressorFactory`.

## Dependency injection

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

See [`Lyo.Compression`](../Lyo.Compression/README.md) for keyed services, `ICompressionResolver`, and `CompressionOptions` in appsettings.json.

**File storage reads:** register `AddBZip2Compressor()` when stored metadata may reference BZip2; [`ICompressionResolver`](../Lyo.Compression/README.md#icompressionresolver-per-algorithm-dispatch) decompresses by metadata on read.
