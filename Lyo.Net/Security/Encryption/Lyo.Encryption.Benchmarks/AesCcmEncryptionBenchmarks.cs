using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Encryption.AesCcm;

namespace Lyo.Encryption.Benchmarks;

[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class AesCcmEncryptionBenchmarks
{
    private byte[] _encryptedLarge = null!;
    private byte[] _encryptedMedium = null!;
    private byte[] _encryptedSmall = null!;
    private AesCcmEncryptionService _encryptionService = null!;
    private byte[] _largeData = null!;
    private byte[] _mediumData = null!;
    private byte[] _smallData = null!;

    [GlobalSetup]
    public void Setup()
    {
        var keyStore = EncryptionBenchmarkSupport.CreateKeyStore();
        _encryptionService = new(keyStore);
        _smallData = new byte[1024];
        _mediumData = new byte[1024 * 1024];
        _largeData = new byte[10 * 1024 * 1024];
        RandomNumberGenerator.Fill(_smallData);
        RandomNumberGenerator.Fill(_mediumData);
        RandomNumberGenerator.Fill(_largeData);
        _encryptedSmall = _encryptionService.Encrypt(_smallData, EncryptionBenchmarkSupport.KeyId);
        _encryptedMedium = _encryptionService.Encrypt(_mediumData, EncryptionBenchmarkSupport.KeyId);
        _encryptedLarge = _encryptionService.Encrypt(_largeData, EncryptionBenchmarkSupport.KeyId);
    }

    [Benchmark] public byte[] Encrypt_1KB() => _encryptionService.Encrypt(_smallData, EncryptionBenchmarkSupport.KeyId);
    [Benchmark] public byte[] Encrypt_1MB() => _encryptionService.Encrypt(_mediumData, EncryptionBenchmarkSupport.KeyId);
    [Benchmark] public byte[] Encrypt_10MB() => _encryptionService.Encrypt(_largeData, EncryptionBenchmarkSupport.KeyId);
    [Benchmark] public byte[] Decrypt_1KB() => _encryptionService.Decrypt(_encryptedSmall, EncryptionBenchmarkSupport.KeyId);
    [Benchmark] public byte[] Decrypt_1MB() => _encryptionService.Decrypt(_encryptedMedium, EncryptionBenchmarkSupport.KeyId);
    [Benchmark] public byte[] Decrypt_10MB() => _encryptionService.Decrypt(_encryptedLarge, EncryptionBenchmarkSupport.KeyId);
}
