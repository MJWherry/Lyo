using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Benchmarking.Data;
using Lyo.Encryption.AesGcmRsa;
using Lyo.Encryption.Rsa;

namespace Lyo.Encryption.Benchmarks;

/// <summary>RSA-only benchmarks (2048-bit OAEP-SHA256). Large payloads use automatic chunking.</summary>
[BenchmarkDescription(
    "RSA-only encrypt/decrypt (2048-bit, OAEP-SHA256) of fixed 1 KB / 64 KB / 1 MB seeded buffers; payloads beyond one RSA block use automatic chunking, so this measures asymmetric-only cost. PEM keys live under the suite IOTemp session.")]
public class RsaEncryptionBenchmarks : LyoBenchmarkBase
{
    private RsaDecryptor _decryptor = null!;
    private byte[] _encryptedLarge = null!;
    private byte[] _encryptedMedium = null!;
    private byte[] _encryptedSmall = null!;
    private RsaEncryptor _encryptor = null!;
    private byte[] _largeData = null!;
    private byte[] _mediumData = null!;
    private byte[] _smallData = null!;

    /// <inheritdoc />
    protected override void OnGlobalSetup()
    {
        var (publicPath, privatePath) = EncryptionBenchmarkSupport.CreateRsaPemFiles(Temp);
        _encryptor = new(publicPath, padding: RSAEncryptionPadding.OaepSHA256);
        _decryptor = new(privatePath, padding: RSAEncryptionPadding.OaepSHA256);
        _smallData = BenchmarkData.DeterministicBytes(1024);
        _mediumData = BenchmarkData.DeterministicBytes(64 * 1024);
        _largeData = BenchmarkData.DeterministicBytes(BenchmarkData.MiB);
    }

    [GlobalSetup(Target = nameof(Decrypt_1KB))]
    public void SetupDecrypt1Kb()
    {
        EnsureGlobalSetup();
        _encryptedSmall = _encryptor.Encrypt(_smallData);
    }

    [GlobalSetup(Target = nameof(Decrypt_64KB))]
    public void SetupDecrypt64Kb()
    {
        EnsureGlobalSetup();
        _encryptedMedium = _encryptor.Encrypt(_mediumData);
    }

    [GlobalSetup(Target = nameof(Decrypt_1MB))]
    public void SetupDecrypt1Mb()
    {
        EnsureGlobalSetup();
        _encryptedLarge = _encryptor.Encrypt(_largeData);
    }

    /// <inheritdoc />
    protected override void OnGlobalCleanup()
    {
        _encryptor?.Dispose();
        _decryptor?.Dispose();
    }

    [Benchmark]
    public byte[] Encrypt_1KB() => _encryptor.Encrypt(_smallData);

    [Benchmark]
    public byte[] Encrypt_64KB() => _encryptor.Encrypt(_mediumData);

    [Benchmark]
    public byte[] Encrypt_1MB() => _encryptor.Encrypt(_largeData);

    [Benchmark]
    public byte[] Decrypt_1KB() => _decryptor.Decrypt(_encryptedSmall);

    [Benchmark]
    public byte[] Decrypt_64KB() => _decryptor.Decrypt(_encryptedMedium);

    [Benchmark]
    public byte[] Decrypt_1MB() => _decryptor.Decrypt(_encryptedLarge);
}

[BenchmarkDescription(
    "Hybrid AES-GCM + RSA envelope encrypt/decrypt of seeded deterministic buffers (100 / 250 / 500 MiB): RSA wraps a per-message AES key while AES-GCM encrypts the bulk payload. PEM keys live under the suite IOTemp session.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Plaintext size: 100, 250, or 500 MiB.")]
public class AesGcmRsaEncryptionBenchmarks : LyoBenchmarkBase
{
    private byte[] _encrypted = null!;
    private AesGcmRsaEncryptionService _encryptionService = null!;
    private byte[] _testData = null!;

    [Params(BenchmarkData.BufferedSize100MiB, BenchmarkData.BufferedSize250MiB, BenchmarkData.BufferedSize500MiB)]
    public int DataSize { get; set; }

    /// <inheritdoc />
    protected override void OnGlobalSetup()
    {
        var (publicPath, privatePath) = EncryptionBenchmarkSupport.CreateRsaPemFiles(Temp);
        _encryptionService = new(publicPath, privatePath, padding: RSAEncryptionPadding.OaepSHA256);
        _testData = BenchmarkData.DeterministicBytes(DataSize);
    }

    [GlobalSetup(Target = nameof(Decrypt))]
    public void SetupDecrypt()
    {
        EnsureGlobalSetup();
        _encrypted = _encryptionService.Encrypt(_testData);
    }

    /// <inheritdoc />
    protected override void OnGlobalCleanup() => _encryptionService?.Dispose();

    [Benchmark]
    public byte[] Encrypt() => _encryptionService.Encrypt(_testData);

    [Benchmark]
    public byte[] Decrypt() => _encryptionService.Decrypt(_encrypted);
}