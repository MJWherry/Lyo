using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Encryption.XChaCha20Poly1305;

namespace Lyo.Encryption.Benchmarks;

[BenchmarkDescription(
    "XChaCha20-Poly1305 (24-byte nonce, explicit key) encrypt and decrypt of seeded deterministic buffers (100 / 250 / 500 MiB); decrypt cases reuse ciphertext from setup.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Plaintext size: 100, 250, or 500 MiB.")]
public class XChaCha20Poly1305EncryptionBenchmarks : LyoBenchmarkBase
{
    private byte[] _encrypted = null!;
    private XChaCha20Poly1305EncryptionService _encryptionService = null!;
    private byte[] _key = null!;
    private byte[] _testData = null!;

    [Params(BenchmarkData.BufferedSize100MiB, BenchmarkData.BufferedSize250MiB, BenchmarkData.BufferedSize500MiB)]
    public int DataSize { get; set; }

    /// <inheritdoc />
    protected override void OnGlobalSetup()
    {
        var keyStore = EncryptionBenchmarkSupport.CreateKeyStore();
        _key = EncryptionBenchmarkSupport.GetSymmetricKey(keyStore);
        _encryptionService = new(keyStore);
        _testData = BenchmarkData.DeterministicBytes(DataSize);
    }

    [GlobalSetup(Target = nameof(Decrypt))]
    public void SetupDecrypt()
    {
        EnsureGlobalSetup();
        _encrypted = _encryptionService.Encrypt(_testData, key: _key);
    }

    [Benchmark]
    public byte[] Encrypt() => _encryptionService.Encrypt(_testData, key: _key);

    [Benchmark]
    public byte[] Decrypt() => _encryptionService.Decrypt(_encrypted, key: _key);
}