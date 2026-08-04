using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Benchmarking.Data;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.AesSiv;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.XChaCha20Poly1305;
using Lyo.Keystore;

namespace Lyo.Encryption.Benchmarks;

/// <summary>Benchmarks comparing bulk symmetric authenticated-encryption algorithms at large buffer sizes.</summary>
/// <remarks>
/// AES-CCM is omitted: with a 12-byte nonce a single CCM packet is capped at ~16 MiB. See <see cref="AesCcmEncryptionBenchmarks"/> for CCM at legal sizes.
/// </remarks>
[ComparisonSuite(Baseline = "AesGcm")]
[BenchmarkDescription(
    "Encrypts and decrypts the same seeded deterministic buffer with AES-GCM, AES-SIV, ChaCha20-Poly1305, and XChaCha20-Poly1305 to compare throughput at each payload size. Decrypt cases reuse a ciphertext produced once in setup. AES-CCM is covered separately (packet-size limit).")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Size of the seeded plaintext/ciphertext buffer (100 MiB, 250 MiB, 500 MiB).")]
[BenchmarkSla(
    MinThroughputMbps = 300, SizeParam = "DataSize", MinThroughputSizeBytes = BenchmarkData.BufferedSize100MiB,
    Standard =
        "Single-pass AEAD with hardware acceleration (AES-GCM, ChaCha20-Poly1305) should sustain >= 300 MB/s for bulk (>= 100 MiB) payloads.")]
public class AlgorithmComparisonBenchmarks : LyoBenchmarkBase
{
    private AesGcmEncryptionService _aesGcmService = null!;
    private AesSivEncryptionService _aesSivService = null!;
    private ChaCha20Poly1305EncryptionService _chachaService = null!;
    private byte[] _encryptedAesGcm = null!;
    private byte[] _encryptedAesSiv = null!;
    private byte[] _encryptedChacha = null!;
    private byte[] _encryptedXChaCha = null!;
    private LocalKeyStore _keyStore = null!;
    private byte[] _testData = null!;
    private byte[] _xChaChaKey = null!;
    private XChaCha20Poly1305EncryptionService _xChaChaService = null!;

    [Params(BenchmarkData.BufferedSize100MiB, BenchmarkData.BufferedSize250MiB, BenchmarkData.BufferedSize500MiB)]
    public int DataSize { get; set; }

    /// <inheritdoc />
    protected override void OnGlobalSetup()
    {
        _keyStore = EncryptionBenchmarkSupport.CreateKeyStore();
        _xChaChaKey = EncryptionBenchmarkSupport.GetSymmetricKey(_keyStore);
        _aesGcmService = new(_keyStore);
        _aesSivService = new(_keyStore);
        _chachaService = new(_keyStore);
        _xChaChaService = new(_keyStore);
        _testData = BenchmarkData.DeterministicBytes(DataSize);
    }

    [GlobalSetup(Target = nameof(AesGcm_Decrypt))]
    public void SetupAesGcmDecrypt()
    {
        EnsureGlobalSetup();
        _encryptedAesGcm = _aesGcmService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);
    }

    [GlobalSetup(Target = nameof(AesSiv_Decrypt))]
    public void SetupAesSivDecrypt()
    {
        EnsureGlobalSetup();
        _encryptedAesSiv = _aesSivService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);
    }

    [GlobalSetup(Target = nameof(ChaCha20Poly1305_Decrypt))]
    public void SetupChaChaDecrypt()
    {
        EnsureGlobalSetup();
        _encryptedChacha = _chachaService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);
    }

    [GlobalSetup(Target = nameof(XChaCha20Poly1305_Decrypt))]
    public void SetupXChaChaDecrypt()
    {
        EnsureGlobalSetup();
        _encryptedXChaCha = _xChaChaService.Encrypt(_testData, key: _xChaChaKey);
    }

    [Benchmark(Baseline = true)]
    [ComparisonAxis("Encrypt")]
    public byte[] AesGcm_Encrypt() => _aesGcmService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    [ComparisonAxis("Encrypt")]
    [BenchmarkSla(
        MinThroughputMbps = 35, SizeParam = "DataSize", MinThroughputSizeBytes = BenchmarkData.BufferedSize100MiB,
        Standard = "AES-SIV is nonce-misuse-resistant (S2V + CTR, two passes) and inherently slower; >= 35 MB/s is acceptable when nonce-reuse safety is the priority.")]
    public byte[] AesSiv_Encrypt() => _aesSivService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    [ComparisonAxis("Encrypt")]
    public byte[] ChaCha20Poly1305_Encrypt() => _chachaService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    [ComparisonAxis("Encrypt")]
    public byte[] XChaCha20Poly1305_Encrypt() => _xChaChaService.Encrypt(_testData, key: _xChaChaKey);

    [Benchmark]
    [ComparisonAxis("Decrypt")]
    public byte[] AesGcm_Decrypt() => _aesGcmService.Decrypt(_encryptedAesGcm, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    [ComparisonAxis("Decrypt")]
    [BenchmarkSla(
        MinThroughputMbps = 35, SizeParam = "DataSize", MinThroughputSizeBytes = BenchmarkData.BufferedSize100MiB,
        Standard = "AES-SIV is nonce-misuse-resistant (S2V + CTR, two passes) and inherently slower; >= 35 MB/s is acceptable when nonce-reuse safety is the priority.")]
    public byte[] AesSiv_Decrypt() => _aesSivService.Decrypt(_encryptedAesSiv, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    [ComparisonAxis("Decrypt")]
    public byte[] ChaCha20Poly1305_Decrypt() => _chachaService.Decrypt(_encryptedChacha, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    [ComparisonAxis("Decrypt")]
    public byte[] XChaCha20Poly1305_Decrypt() => _xChaChaService.Decrypt(_encryptedXChaCha, key: _xChaChaKey);
}
