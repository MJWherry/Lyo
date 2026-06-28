using System;
using System.Security.Cryptography;
using System.Text;

namespace Lyo.Benchmarking.Data;

/// <summary>Shared payload generators used across benchmark suites (de-duplicates per-project helpers).</summary>
public static class BenchmarkData
{
    private const string Seed = "the quick brown fox jumps over the lazy dog 0123456789 ";

    /// <summary>Builds a repeating, highly compressible ASCII string of exactly <paramref name="sizeBytes" /> characters.</summary>
    public static string CompressibleString(int sizeBytes)
    {
        if (sizeBytes <= 0)
            return string.Empty;

        var builder = new StringBuilder(sizeBytes + Seed.Length);
        while (builder.Length < sizeBytes)
            builder.Append(Seed);
        return builder.ToString(0, sizeBytes);
    }

    /// <summary>Fills a new buffer of <paramref name="sizeBytes" /> with cryptographically random (incompressible) bytes.</summary>
    public static byte[] RandomBytes(int sizeBytes)
    {
        var buffer = new byte[Math.Max(0, sizeBytes)];
        if (buffer.Length > 0)
            RandomNumberGenerator.Fill(buffer);
        return buffer;
    }
}
