using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Encryption.AesSiv;

namespace Lyo.Encryption.Benchmarks;

[BenchmarkDescription(
    "AES-SIV (nonce-misuse-resistant) encrypt and decrypt of seeded deterministic buffers (100 / 250 / 500 MiB); decrypt cases reuse ciphertext from setup.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Plaintext size: 100, 250, or 500 MiB.")]
public class AesSivEncryptionBenchmarks : LyoBenchmarkBase
{
    private byte[] _encrypted = null!;
    private AesSivEncryptionService _encryptionService = null!;
    private byte[] _testData = null!;

    [Params(BenchmarkData.BufferedSize100MiB, BenchmarkData.BufferedSize250MiB, BenchmarkData.BufferedSize500MiB)]
    public int DataSize { get; set; }

    /// <inheritdoc />
    protected override void OnGlobalSetup()
    {
        _encryptionService = new(EncryptionBenchmarkSupport.CreateKeyStore());
        _testData = BenchmarkData.DeterministicBytes(DataSize);
    }

    [GlobalSetup(Target = nameof(Decrypt))]
    public void SetupDecrypt()
    {
        EnsureGlobalSetup();
        _encrypted = _encryptionService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);
    }

    [Benchmark]
    public byte[] Encrypt() => _encryptionService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    public byte[] Decrypt() => _encryptionService.Decrypt(_encrypted, EncryptionBenchmarkSupport.KeyId);
}