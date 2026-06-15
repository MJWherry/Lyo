using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Encryption.AesCcm;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.AesSiv;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.XChaCha20Poly1305;
using Lyo.Keystore;

namespace Lyo.Encryption.Benchmarks;

/// <summary>Benchmarks comparing all symmetric authenticated-encryption algorithms.</summary>
[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class AlgorithmComparisonBenchmarks
{
    private AesGcmEncryptionService _aesGcmService = null!;
    private AesCcmEncryptionService _aesCcmService = null!;
    private AesSivEncryptionService _aesSivService = null!;
    private ChaCha20Poly1305EncryptionService _chachaService = null!;
    private XChaCha20Poly1305EncryptionService _xChaChaService = null!;
    private byte[] _encryptedAesGcm = null!;
    private byte[] _encryptedAesCcm = null!;
    private byte[] _encryptedAesSiv = null!;
    private byte[] _encryptedChacha = null!;
    private byte[] _encryptedXChaCha = null!;
    private LocalKeyStore _keyStore = null!;
    private byte[] _xChaChaKey = null!;
    private byte[] _testData = null!;

    [Params(1024, 1024 * 1024, 10 * 1024 * 1024, 100 * 1024 * 1024)] // 1 KB, 1 MB, 10 MB, 100 MB
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _keyStore = EncryptionBenchmarkSupport.CreateKeyStore();
        _xChaChaKey = EncryptionBenchmarkSupport.GetSymmetricKey(_keyStore);
        _aesGcmService = new(_keyStore);
        _aesCcmService = new(_keyStore);
        _aesSivService = new(_keyStore);
        _chachaService = new(_keyStore);
        _xChaChaService = new(_keyStore);
        _testData = new byte[DataSize];
        RandomNumberGenerator.Fill(_testData);
        _encryptedAesGcm = _aesGcmService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);
        _encryptedAesCcm = _aesCcmService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);
        _encryptedAesSiv = _aesSivService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);
        _encryptedChacha = _chachaService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);
        _encryptedXChaCha = _xChaChaService.Encrypt(_testData, key: _xChaChaKey);
    }

    [Benchmark(Baseline = true)]
    public byte[] AesGcm_Encrypt() => _aesGcmService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    public byte[] AesCcm_Encrypt() => _aesCcmService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    public byte[] AesSiv_Encrypt() => _aesSivService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    public byte[] ChaCha20Poly1305_Encrypt() => _chachaService.Encrypt(_testData, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    public byte[] XChaCha20Poly1305_Encrypt() => _xChaChaService.Encrypt(_testData, key: _xChaChaKey);

    [Benchmark]
    public byte[] AesGcm_Decrypt() => _aesGcmService.Decrypt(_encryptedAesGcm, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    public byte[] AesCcm_Decrypt() => _aesCcmService.Decrypt(_encryptedAesCcm, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    public byte[] AesSiv_Decrypt() => _aesSivService.Decrypt(_encryptedAesSiv, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    public byte[] ChaCha20Poly1305_Decrypt() => _chachaService.Decrypt(_encryptedChacha, EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    public byte[] XChaCha20Poly1305_Decrypt() => _xChaChaService.Decrypt(_encryptedXChaCha, key: _xChaChaKey);
}
