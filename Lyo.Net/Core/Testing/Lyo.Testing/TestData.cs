using Lyo.Exceptions;
using Lyo.Streams;

namespace Lyo.Testing;

/// <summary>
/// Seeded, reproducible byte payloads for xUnit suites. Prefer this over unseeded <see cref="Random" /> or <see cref="System.Security.Cryptography.RandomNumberGenerator" />
/// so tests stay consistent across runs.
/// </summary>
/// <remarks><see cref="Seed" /> matches <c>Lyo.Benchmark.Data.BenchmarkData.PayloadSeed</c> and <see cref="DeterministicPayloadStream.DefaultSeed" />.</remarks>
public static class TestData
{
    /// <summary>Shared payload seed (<c>0x4C594F42</c> / "LYOB"). Same value as benchmark suites.</summary>
    public const int Seed = DeterministicPayloadStream.DefaultSeed;

    /// <summary>Fills <paramref name="buffer" /> with deterministic bytes from <paramref name="seed" /> (defaults to <see cref="Seed" />).</summary>
    public static void Fill(Span<byte> buffer, int seed = Seed) => DeterministicPayloadStream.Fill(buffer, seed);

    /// <summary>Allocates <paramref name="sizeBytes" /> bytes filled via <see cref="Fill" />.</summary>
    public static byte[] Create(int sizeBytes, int seed = Seed)
    {
        ArgumentHelpers.ThrowIfNegative(sizeBytes);
        var buffer = new byte[sizeBytes];
        Fill(buffer, seed);
        return buffer;
    }
}