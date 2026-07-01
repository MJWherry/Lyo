using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Common.Enums;

namespace Lyo.Hashing.Benchmarks;

/// <summary>Benchmarks hex encode/decode round-trips over digest-sized buffers.</summary>
[BenchmarkDescription("Hex encode (lowercase) and decode of digest-sized random buffers; isolates the encoding cost from the hash itself.")]
[BenchmarkParameter("DigestSize", Unit = "bytes", Description = "Length of the digest buffer being encoded/decoded (32 = SHA-256, 64 = SHA-512).")]
[BenchmarkSla(MaxMeanUs = 5, Standard = "Hex encode/decode of a 32-64 byte digest is a trivial transform and should stay well under 5 microseconds.")]
public class HexEncodingBenchmarks
{
    private readonly IHashingService _hashing = HashingService.Shared;
    private byte[] _digest = null!;
    private string _hex = null!;

    [Params(32, 64)] // SHA-256 and SHA-512 digest sizes
    public int DigestSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _digest = new byte[DigestSize];
        RandomNumberGenerator.Fill(_digest);
        _hex = _hashing.ToHex(_digest, TextLetterCase.Lower);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Encode a digest buffer to a lowercase hex string (baseline).")]
    public string ToHex() => _hashing.ToHex(_digest, TextLetterCase.Lower);

    [Benchmark]
    [BenchmarkDescription("Parse a lowercase hex string back into the digest bytes.")]
    public byte[] ParseHex() => _hashing.ParseHex(_hex);
}