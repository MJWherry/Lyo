using System.Security.Cryptography;
using System.Text;
using Lyo.Streams;
using Lyo.Exceptions;

namespace Lyo.Benchmark.Data;

/// <summary>Payload helpers shared by every benchmark suite (avoids repeating per-project generators).</summary>
public static class BenchmarkData
{
    /// <summary>
    /// Fixed seed for deterministic, incompressible payload bytes used by every suite. Matches <see cref="DeterministicPayloadStream.DefaultSeed" /> and
    /// <c>Lyo.Testing.TestData.Seed</c>.
    /// </summary>
    public const int PayloadSeed = DeterministicPayloadStream.DefaultSeed;

    /// <summary>One mebibyte, in bytes.</summary>
    public const int MiB = 1024 * 1024;

    /// <summary>Small in-memory sizes for algorithms with packet caps (AES-CCM is about 16 MiB max with a 12-byte nonce).</summary>
    public const int BufferedSize1MiB = MiB;

    /// <summary>In-memory payload: 4 MiB.</summary>
    public const int BufferedSize4MiB = 4 * MiB;

    /// <summary>In-memory payload: 15 MiB (under AES-CCM's 2^24−1 single-packet ceiling).</summary>
    public const int BufferedSize15MiB = 15 * MiB;

    /// <summary>In-memory payload sizes: 100 / 250 / 500 MiB.</summary>
    public const int BufferedSize100MiB = 100 * MiB;

    /// <summary>In-memory payload: 250 MiB.</summary>
    public const int BufferedSize250MiB = 250 * MiB;

    /// <summary>In-memory payload: 500 MiB.</summary>
    public const int BufferedSize500MiB = 500 * MiB;

    /// <summary>Streaming payload: 100 MiB.</summary>
    public const long StreamingSize100MiB = 100L * MiB;

    /// <summary>Streaming payload: 250 MiB.</summary>
    public const long StreamingSize250MiB = 250L * MiB;

    /// <summary>Streaming payload: 500 MiB.</summary>
    public const long StreamingSize500MiB = 500L * MiB;

    /// <summary>Streaming payload: 750 MiB.</summary>
    public const long StreamingSize750MiB = 750L * MiB;

    /// <summary>Streaming payload: 1 GiB.</summary>
    public const long StreamingSize1GiB = 1024L * MiB;

    /// <summary>Streaming payload: 1.5 GiB.</summary>
    public const long StreamingSize15GiB = 1536L * MiB;

    /// <summary>Streaming payload: 2 GiB.</summary>
    public const long StreamingSize2GiB = 2048L * MiB;

    private const string CompressibleSeed = "the quick brown fox jumps over the lazy dog 0123456789 ";

    /// <summary>Builds a repeating, highly compressible ASCII string whose length is exactly <paramref name="sizeBytes" />.</summary>
    public static string CompressibleString(int sizeBytes)
    {
        if (sizeBytes <= 0)
            return string.Empty;

        var builder = new StringBuilder(sizeBytes + CompressibleSeed.Length);
        while (builder.Length < sizeBytes)
            builder.Append(CompressibleSeed);

        return builder.ToString(0, sizeBytes);
    }

    /// <summary>Allocates a buffer of <paramref name="sizeBytes" /> filled with cryptographically random (incompressible) bytes.</summary>
    /// <remarks>For encrypt/compress suites prefer <see cref="DeterministicBytes" /> so timings stay comparable across runs.</remarks>
    public static byte[] RandomBytes(int sizeBytes)
    {
        var buffer = new byte[Math.Max(0, sizeBytes)];
        if (buffer.Length > 0)
            RandomNumberGenerator.Fill(buffer);

        return buffer;
    }

    /// <summary>
    /// Writes deterministic bytes from <see cref="PayloadSeed" /> into <paramref name="buffer" />. The same seed and length always yield the same sequence, so suite results
    /// stay comparable across runs and algorithms.
    /// </summary>
    public static void FillDeterministic(Span<byte> buffer) => DeterministicPayloadStream.Fill(buffer);

    /// <summary>Allocates <paramref name="sizeBytes" /> filled by <see cref="FillDeterministic" />.</summary>
    public static byte[] DeterministicBytes(int sizeBytes) => DeterministicPayloadStream.CreateBytes(sizeBytes);

    /// <summary>
    /// Writes exactly <paramref name="size" /> deterministic bytes (from <see cref="PayloadSeed" />) onto <paramref name="stream" /> using a reusable chunk buffer. Fits
    /// multi-gigabyte streaming payloads without keeping the whole buffer in memory.
    /// </summary>
    public static void WriteDeterministic(Stream stream, long size, int bufferSize = MiB)
    {
        ArgumentHelpers.ThrowIfNull(stream);
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