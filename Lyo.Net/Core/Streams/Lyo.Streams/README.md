# Lyo.Streams

Common stream implementations including **TeeStream**, **CountingStream**, **ProgressStream**, **ConcatenatedStream**, etc. (**incremental hashing** lives in **`Lyo.Hashing`**: **`HashingStream`**).

## Features

- **TeeStream** – Duplicate stream output to multiple destinations
- **CountingStream** – Track bytes read or written
- **ProgressStream** – Report progress during stream operations
- **ConcatenatedStream** – Sequentially read from multiple streams
- **StreamExtensions** – `CopyToAsync` with optional `IProgress<long>` (cumulative bytes written)
- **StreamChunkSizeHelper** – Determine optimal buffer size for stream operations

## Quick Start

```csharp
using Lyo.Streams;
using Lyo.Hashing;
using System.Security.Cryptography;

// Hash while reading — use Lyo.Hashing.HashingStream
using var hashingStream = new HashingStream(sourceStream, SHA256.Create());
await hashingStream.CopyToAsync(destinationStream);
var hash = hashingStream.GetHash();

// Tee to multiple outputs
using var tee = new TeeStream(inputStream, stream1, stream2);
await tee.CopyToAsync(outputStream);

// Concatenate multiple streams
var streams = new[] { stream1, stream2, stream3 };
using var concatenated = new ConcatenatedStream(streams);
await concatenated.CopyToAsync(outputStream);

// Copy with progress (IProgress<long> reports cumulative bytes written)
var progress = new Progress<long>(bytes => Console.WriteLine($"Copied {bytes} bytes"));
await source.CopyToAsync(destination, bufferSize: 81920, progress: progress);
```

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Streams.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package          | Version |
|------------------|---------|
| `System.Buffers` | `4.6.0` |

### Project references

- `Lyo.Exceptions`

## Public API (generated)

Top-level `public` types in `*.cs` (*7*). Nested types and file-scoped namespaces may omit some entries.

- `ConcatenatedStream`
- `CountingStream`
- `ProgressStream`
- `StreamChunkSizeHelper`
- `StreamChunkSizeOptions`
- `StreamExtensions`
- `TeeStream`

<!-- LYO_README_SYNC:END -->

