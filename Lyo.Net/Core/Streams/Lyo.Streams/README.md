# Lyo.Streams

Stream wrappers including `TeeStream`, `CountingStream`, `ProgressStream`, and `ConcatenatedStream`. Incremental hashing lives in `Lyo.Hashing` (`HashingStream`).

## Features

- **TeeStream.** Fan stream output out to more than one destination.
- **CountingStream.** Count bytes read or written.
- **ProgressStream.** Report progress while the stream is in use.
- **ConcatenatedStream.** Read several streams one after another.
- **DeterministicPayloadStream.** From a seed, yields a fixed-length deterministic byte sequence without allocating the full buffer. `DefaultSeed` is shared with `Lyo.Testing.TestData` and `BenchmarkData`.
- **NullingStream.** Write-only sink that discards bytes (throughput / drain consumer).
- **StreamExtensions.** `CopyToAsync` accepts an optional `IProgress<long>` that reports cumulative bytes written.
- **StreamChunkSizeHelper.** Choose a buffer size for stream work.

## Examples

### First steps

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

### Deterministic payload and a nulling sink

```csharp
using Lyo.Streams;

// Generate 100 MiB of seeded bytes without allocating a 100 MiB array
await using var input = new DeterministicPayloadStream(length: 100L * 1024 * 1024);
await using var sink = new NullingStream();
await input.CopyToAsync(sink);
Console.WriteLine($"Drained {sink.BytesWritten} bytes");
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `System.Buffers` `4.6.1` (direct, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (direct, microsoft, netstandard2.0)