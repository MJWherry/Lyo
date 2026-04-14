using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Keystore;

namespace Lyo.Encryption.Benchmarks;

/// <summary>Benchmarks comparing AES-GCM vs ChaCha20Poly1305 performance</summary>
[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class AlgorithmComparisonBenchmarks
{
    private const string KeyId = "benchmark-key";
    private AesGcmEncryptionService _aesGcmService = null!;
    private ChaCha20Poly1305EncryptionService _chachaService = null!;
    private byte[] _encryptedAesGcm = null!;
    private byte[] _encryptedChacha = null!;
    private LocalKeyStore _keyStore = null!;
    private byte[] _testData = null!;

    [Params(1024, 1024 * 1024, 10 * 1024 * 1024, 100 * 1024 * 1024)] // 1 KB, 1 MB, 10 MB, 100 MB
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _keyStore = new();
        _keyStore.UpdateKeyFromString(KeyId, "benchmark-test-key-32-bytes-long!");
        _aesGcmService = new(_keyStore);
        _chachaService = new(_keyStore);
        _testData = new byte[DataSize];
        RandomNumberGenerator.Fill(_testData);
        _encryptedAesGcm = _aesGcmService.Encrypt(_testData, KeyId);
        _encryptedChacha = _chachaService.Encrypt(_testData, KeyId);
    }

    [Benchmark(Baseline = true)]
    public byte[] AesGcm_Encrypt() => _aesGcmService.Encrypt(_testData, KeyId);

    [Benchmark]
    public byte[] ChaCha20Poly1305_Encrypt() => _chachaService.Encrypt(_testData, KeyId);

    [Benchmark]
    public byte[] AesGcm_Decrypt() => _aesGcmService.Decrypt(_encryptedAesGcm, KeyId);

    [Benchmark]
    public byte[] ChaCha20Poly1305_Decrypt() => _chachaService.Decrypt(_encryptedChacha, KeyId);
}