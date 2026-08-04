using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Benchmarking.Data;
using Lyo.Encryption.AesCcm;

namespace Lyo.Encryption.Benchmarks;

[BenchmarkDescription(
    "AES-CCM encrypt and decrypt of seeded deterministic buffers (1 / 4 / 15 MiB). Single-shot CCM with a 12-byte nonce cannot exceed ~16 MiB per packet; use streaming benches for larger payloads.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Plaintext size: 1, 4, or 15 MiB (CCM packet limit).")]
public class AesCcmEncryptionBenchmarks : LyoBenchmarkBase
{
    private byte[] _encrypted = null!;
    private AesCcmEncryptionService _encryptionService = null!;
    private byte[] _testData = null!;

    [Params(BenchmarkData.BufferedSize1MiB, BenchmarkData.BufferedSize4MiB, BenchmarkData.BufferedSize15MiB)]
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
