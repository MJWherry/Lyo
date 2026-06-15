using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Encryption.AesGcmRsa;
using Lyo.Encryption.Rsa;

namespace Lyo.Encryption.Benchmarks;

/// <summary>RSA-only benchmarks (2048-bit OAEP-SHA256). Large payloads use automatic chunking.</summary>
[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class RsaEncryptionBenchmarks
{
    private string _privatePath = null!;
    private string _publicPath = null!;
    private RsaEncryptionService _encryptionService = null!;
    private byte[] _encryptedLarge = null!;
    private byte[] _encryptedMedium = null!;
    private byte[] _encryptedSmall = null!;
    private byte[] _largeData = null!;
    private byte[] _mediumData = null!;
    private byte[] _smallData = null!;

    [GlobalSetup]
    public void Setup()
    {
        (_publicPath, _privatePath) = EncryptionBenchmarkSupport.CreateRsaPemFiles();
        _encryptionService = new(_publicPath, _privatePath, padding: RSAEncryptionPadding.OaepSHA256);
        _smallData = new byte[1024];
        _mediumData = new byte[64 * 1024];
        _largeData = new byte[1024 * 1024];
        RandomNumberGenerator.Fill(_smallData);
        RandomNumberGenerator.Fill(_mediumData);
        RandomNumberGenerator.Fill(_largeData);
        _encryptedSmall = _encryptionService.Encrypt(_smallData);
        _encryptedMedium = _encryptionService.Encrypt(_mediumData);
        _encryptedLarge = _encryptionService.Encrypt(_largeData);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _encryptionService.Dispose();
        EncryptionBenchmarkSupport.TryDelete(_publicPath, _privatePath);
    }

    [Benchmark] public byte[] Encrypt_1KB() => _encryptionService.Encrypt(_smallData);
    [Benchmark] public byte[] Encrypt_64KB() => _encryptionService.Encrypt(_mediumData);
    [Benchmark] public byte[] Encrypt_1MB() => _encryptionService.Encrypt(_largeData);
    [Benchmark] public byte[] Decrypt_1KB() => _encryptionService.Decrypt(_encryptedSmall);
    [Benchmark] public byte[] Decrypt_64KB() => _encryptionService.Decrypt(_encryptedMedium);
    [Benchmark] public byte[] Decrypt_1MB() => _encryptionService.Decrypt(_encryptedLarge);
}

[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class AesGcmRsaEncryptionBenchmarks
{
    private string _privatePath = null!;
    private string _publicPath = null!;
    private AesGcmRsaEncryptionService _encryptionService = null!;
    private byte[] _encryptedLarge = null!;
    private byte[] _encryptedMedium = null!;
    private byte[] _encryptedSmall = null!;
    private byte[] _largeData = null!;
    private byte[] _mediumData = null!;
    private byte[] _smallData = null!;

    [GlobalSetup]
    public void Setup()
    {
        (_publicPath, _privatePath) = EncryptionBenchmarkSupport.CreateRsaPemFiles();
        _encryptionService = new(_publicPath, _privatePath, padding: RSAEncryptionPadding.OaepSHA256);
        _smallData = new byte[1024];
        _mediumData = new byte[1024 * 1024];
        _largeData = new byte[10 * 1024 * 1024];
        RandomNumberGenerator.Fill(_smallData);
        RandomNumberGenerator.Fill(_mediumData);
        RandomNumberGenerator.Fill(_largeData);
        _encryptedSmall = _encryptionService.Encrypt(_smallData);
        _encryptedMedium = _encryptionService.Encrypt(_mediumData);
        _encryptedLarge = _encryptionService.Encrypt(_largeData);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _encryptionService.Dispose();
        EncryptionBenchmarkSupport.TryDelete(_publicPath, _privatePath);
    }

    [Benchmark] public byte[] Encrypt_1KB() => _encryptionService.Encrypt(_smallData);
    [Benchmark] public byte[] Encrypt_1MB() => _encryptionService.Encrypt(_mediumData);
    [Benchmark] public byte[] Encrypt_10MB() => _encryptionService.Encrypt(_largeData);
    [Benchmark] public byte[] Decrypt_1KB() => _encryptionService.Decrypt(_encryptedSmall);
    [Benchmark] public byte[] Decrypt_1MB() => _encryptionService.Decrypt(_encryptedMedium);
    [Benchmark] public byte[] Decrypt_10MB() => _encryptionService.Decrypt(_encryptedLarge);
}
