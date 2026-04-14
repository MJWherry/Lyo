using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Encryption.AesGcm;
using Lyo.Keystore;

namespace Lyo.Encryption.Benchmarks;

[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class AesGcmEncryptionBenchmarks
{
    private const string KeyId = "benchmark-key";
    private byte[] _encryptedLarge = null!;
    private byte[] _encryptedMedium = null!;
    private byte[] _encryptedSmall = null!;
    private AesGcmEncryptionService _encryptionService = null!;
    private LocalKeyStore _keyStore = null!;
    private byte[] _largeData = null!;
    private byte[] _mediumData = null!;
    private byte[] _smallData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _keyStore = new();
        _keyStore.UpdateKeyFromString(KeyId, "benchmark-test-key-32-bytes-long!");
        _encryptionService = new(_keyStore);

        // Generate test data
        _smallData = new byte[1024]; // 1 KB
        _mediumData = new byte[1024 * 1024]; // 1 MB
        _largeData = new byte[10 * 1024 * 1024]; // 10 MB
        RandomNumberGenerator.Fill(_smallData);
        RandomNumberGenerator.Fill(_mediumData);
        RandomNumberGenerator.Fill(_largeData);

        // Pre-encrypt data for decryption benchmarks
        _encryptedSmall = _encryptionService.Encrypt(_smallData, KeyId);
        _encryptedMedium = _encryptionService.Encrypt(_mediumData, KeyId);
        _encryptedLarge = _encryptionService.Encrypt(_largeData, KeyId);
    }

    [Benchmark]
    public byte[] Encrypt_1KB() => _encryptionService.Encrypt(_smallData, KeyId);

    [Benchmark]
    public byte[] Encrypt_1MB() => _encryptionService.Encrypt(_mediumData, KeyId);

    [Benchmark]
    public byte[] Encrypt_10MB() => _encryptionService.Encrypt(_largeData, KeyId);

    [Benchmark]
    public byte[] Decrypt_1KB() => _encryptionService.Decrypt(_encryptedSmall, KeyId);

    [Benchmark]
    public byte[] Decrypt_1MB() => _encryptionService.Decrypt(_encryptedMedium, KeyId);

    [Benchmark]
    public byte[] Decrypt_10MB() => _encryptionService.Decrypt(_encryptedLarge, KeyId);
}