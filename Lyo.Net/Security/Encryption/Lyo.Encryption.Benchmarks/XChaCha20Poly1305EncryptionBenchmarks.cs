using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Encryption.XChaCha20Poly1305;

namespace Lyo.Encryption.Benchmarks;

[BenchmarkDescription(
    "XChaCha20-Poly1305 (24-byte nonce, explicit key) encrypt and decrypt of fixed 1 KB / 1 MB / 10 MB random buffers; decrypt cases reuse ciphertext from setup.")]
public class XChaCha20Poly1305EncryptionBenchmarks
{
    private byte[] _encryptedLarge = null!;
    private byte[] _encryptedMedium = null!;
    private byte[] _encryptedSmall = null!;
    private XChaCha20Poly1305EncryptionService _encryptionService = null!;
    private byte[] _key = null!;
    private byte[] _largeData = null!;
    private byte[] _mediumData = null!;
    private byte[] _smallData = null!;

    [GlobalSetup]
    public void Setup()
    {
        var keyStore = EncryptionBenchmarkSupport.CreateKeyStore();
        _key = EncryptionBenchmarkSupport.GetSymmetricKey(keyStore);
        _encryptionService = new(keyStore);
        _smallData = new byte[1024];
        _mediumData = new byte[1024 * 1024];
        _largeData = new byte[10 * 1024 * 1024];
        RandomNumberGenerator.Fill(_smallData);
        RandomNumberGenerator.Fill(_mediumData);
        RandomNumberGenerator.Fill(_largeData);
        // XChaCha uses 24-byte nonces; benchmark with explicit key (same as unit tests).
        _encryptedSmall = _encryptionService.Encrypt(_smallData, key: _key);
        _encryptedMedium = _encryptionService.Encrypt(_mediumData, key: _key);
        _encryptedLarge = _encryptionService.Encrypt(_largeData, key: _key);
    }

    [Benchmark]
    public byte[] Encrypt_1KB() => _encryptionService.Encrypt(_smallData, key: _key);

    [Benchmark]
    public byte[] Encrypt_1MB() => _encryptionService.Encrypt(_mediumData, key: _key);

    [Benchmark]
    public byte[] Encrypt_10MB() => _encryptionService.Encrypt(_largeData, key: _key);

    [Benchmark]
    public byte[] Decrypt_1KB() => _encryptionService.Decrypt(_encryptedSmall, key: _key);

    [Benchmark]
    public byte[] Decrypt_1MB() => _encryptionService.Decrypt(_encryptedMedium, key: _key);

    [Benchmark]
    public byte[] Decrypt_10MB() => _encryptionService.Decrypt(_encryptedLarge, key: _key);
}