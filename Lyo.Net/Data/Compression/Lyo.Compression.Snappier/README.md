# Lyo.Compression.Snappier

Snappy compression addon for `Lyo.Compression`. Registers a Snappier `ICompressorFactory`.

## Dependency injection

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

See [`Lyo.Compression`](../Lyo.Compression/README.md) for the full DI, `ICompressionResolver`, and configuration guide.

**File storage reads:** register `AddSnappierCompressor()` when stored metadata may reference Snappy; decompression uses `ICompressionResolver`, not only the service default codec.
