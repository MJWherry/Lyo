using System.Security.Cryptography;
using System.Text;
using Lyo.Streams;

namespace Lyo.Benchmark.Data;

/// <summary>Shared payload generators used across benchmark suites (de-duplicates per-project helpers).</summary>
public static class BenchmarkData
{
    /// <summary>
    /// Fixed seed for deterministic, incompressible payload bytes shared by all benchmark suites. Same value as <see cref="DeterministicPayloadStream.DefaultSeed" /> and
    /// <c>Lyo.Testing.TestData.Seed</c>.
    /// </summary>
    public const int PayloadSeed = DeterministicPayloadStream.DefaultSeed;

    /// <summary>One mebibyte in bytes.</summary>
    public const int MiB = 1024 * 1024;

    /// <summary>Small buffered sizes for algorithms with packet limits (e.g. AES-CCM ~16 MiB max with a 12-byte nonce).</summary>
    public const int BufferedSize1MiB = MiB;

    /// <summary>Buffered payload size: 4 MiB.</summary>
    public const int BufferedSize4MiB = 4 * MiB;

    /// <summary>Buffered payload size: 15 MiB (under AES-CCM's 2^24−1 single-packet ceiling).</summary>
    public const int BufferedSize15MiB = 15 * MiB;

    /// <summary>Buffered (in-memory) payload sizes: 100 / 250 / 500 MiB.</summary>
    public const int BufferedSize100MiB = 100 * MiB;

    /// <summary>Buffered payload size: 250 MiB.</summary>
    public const int BufferedSize250MiB = 250 * MiB;

    /// <summary>Buffered payload size: 500 MiB.</summary>
    public const int BufferedSize500MiB = 500 * MiB;

    /// <summary>Streaming payload size: 100 MiB.</summary>
    public const long StreamingSize100MiB = 100L * MiB;

    /// <summary>Streaming payload size: 250 MiB.</summary>
    public const long StreamingSize250MiB = 250L * MiB;

    /// <summary>Streaming payload size: 500 MiB.</summary>
    public const long StreamingSize500MiB = 500L * MiB;

    /// <summary>Streaming payload size: 750 MiB.</summary>
    public const long StreamingSize750MiB = 750L * MiB;

    /// <summary>Streaming payload size: 1 GiB.</summary>
    public const long StreamingSize1GiB = 1024L * MiB;

    /// <summary>Streaming payload size: 1.5 GiB.</summary>
    public const long StreamingSize15GiB = 1536L * MiB;

    /// <summary>Streaming payload size: 2 GiB.</summary>
    public const long StreamingSize2GiB = 2048L * MiB;

    private const string CompressibleSeed = "the quick brown fox jumps over the lazy dog 0123456789 ";

    /// <summary>Builds a repeating, highly compressible ASCII string of exactly <paramref name="sizeBytes" /> characters.</summary>
    public static string CompressibleString(int sizeBytes)
    {
        if (sizeBytes <= 0)
            return string.Empty;

        var builder = new StringBuilder(sizeBytes + CompressibleSeed.Length);
        while (builder.Length < sizeBytes)
            builder.Append(CompressibleSeed);

        return builder.ToString(0, sizeBytes);
    }

    /// <summary>Fills a new buffer of <paramref name="sizeBytes" /> with cryptographically random (incompressible) bytes.</summary>
    /// <remarks>Prefer <see cref="DeterministicBytes" /> for enc/comp suites so timings stay comparable across runs.</remarks>
    public static byte[] RandomBytes(int sizeBytes)
    {
        var buffer = new byte[Math.Max(0, sizeBytes)];
        if (buffer.Length > 0)
            RandomNumberGenerator.Fill(buffer);

        return buffer;
    }

    /// <summary>
    /// Fills <paramref name="buffer" /> with deterministic bytes from <see cref="PayloadSeed" />. The same seed and length always produce the same sequence so suite results are
    /// comparable across runs and algorithms.
    /// </summary>
    public static void FillDeterministic(Span<byte> buffer) => DeterministicPayloadStream.Fill(buffer, PayloadSeed);

    /// <summary>Allocates a buffer of <paramref name="sizeBytes" /> filled via <see cref="FillDeterministic" />.</summary>
    public static byte[] DeterministicBytes(int sizeBytes) => DeterministicPayloadStream.CreateBytes(sizeBytes, PayloadSeed);

    /// <summary>
    /// Writes exactly <paramref name="size" /> deterministic bytes (from <see cref="PayloadSeed" />) to <paramref name="stream" /> using a reusable chunk buffer. Suitable for
    /// multi-gigabyte streaming payloads without holding the full buffer in memory.
    /// </summary>
    public static void WriteDeterministic(Stream stream, long size, int bufferSize = MiB)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (size <= 0)
            return;

        if (bufferSize <= 0)
            bufferSize = MiB;

        var rng = new Random(PayloadSeed);
        var buffer = new byte[bufferSize];
        var remaining = size;
        while (remaining > 0) {
            var toWrite = (int)Math.Min(remaining, buffer.Length);
            rng.NextBytes(buffer.AsSpan(0, toWrite));
            stream.Write(buffer, 0, toWrite);
            remaining -= toWrite;
        }
    }
}
