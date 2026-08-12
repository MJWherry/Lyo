using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.AesSiv;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.KeyStore;
using Lyo.Streams;

namespace Lyo.Encryption.Benchmarks;

/// <summary>Large-payload encrypt/decrypt benchmarks for stream and file APIs.</summary>
[BenchmarkDescription(
    "Encrypt/decrypt at 100 MiB–2 GiB with AES-GCM, ChaCha20-Poly1305, and AES-SIV-512. Stream methods use DeterministicPayloadStream input and NullingStream output; file methods use IOTemp paths. Decrypt setup reuses pre-encrypted IOTemp files.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Plaintext size: 100, 250, 500, 750 MiB, 1 GiB, 1.5 GiB, 2 GiB.")]
public class LargeFileStreamingBenchmarks : LyoBenchmarkBase
{
    private const int ChunkSize = BenchmarkData.MiB;

    private AesGcmEncryptionService _aesGcmService = null!;
    private ChaCha20Poly1305EncryptionService _chachaService = null!;
    private string _encryptedAesGcmPath = null!;
    private string _encryptedChachaPath = null!;
    private string _encryptedSivPath = null!;
    private LocalKeyStore _keyStore = null!;
    private string _plaintextPath = null!;
    private byte[] _sivKey = null!;
    private AesSivEncryptionService _sivService = null!;

    [Params(
        BenchmarkData.StreamingSize100MiB, BenchmarkData.StreamingSize250MiB, BenchmarkData.StreamingSize500MiB, BenchmarkData.StreamingSize750MiB, BenchmarkData.StreamingSize1GiB,
        BenchmarkData.StreamingSize15GiB, BenchmarkData.StreamingSize2GiB)]
    public long DataSize { get; set; }

    /// <inheritdoc />
    protected override void OnGlobalSetup()
    {
        _keyStore = EncryptionBenchmarkSupport.CreateKeyStore();
        _aesGcmService = new(_keyStore);
        _chachaService = new(_keyStore);
        _sivService = new(_keyStore, AesSivKeySizeBits.Bits512);
        _sivKey = BenchmarkData.DeterministicBytes(64);
        _plaintextPath = CreateSeededFilePath(DataSize);
    }

    [GlobalSetup(Targets = [nameof(DecryptStream_AesGcm), nameof(DecryptFile_AesGcm)])]
    public void SetupAesGcmDecrypt()
    {
        EnsureGlobalSetup();
        _encryptedAesGcmPath = CreateTempOutputPath();
        _aesGcmService.EncryptFileAsync(_plaintextPath, _encryptedAesGcmPath, EncryptionBenchmarkSupport.KeyId).GetAwaiter().GetResult();
    }

    [GlobalSetup(Targets = [nameof(DecryptStream_ChaCha), nameof(DecryptFile_ChaCha)])]
    public void SetupChaChaDecrypt()
    {
        EnsureGlobalSetup();
        _encryptedChachaPath = CreateTempOutputPath();
        _chachaService.EncryptFileAsync(_plaintextPath, _encryptedChachaPath, EncryptionBenchmarkSupport.KeyId).GetAwaiter().GetResult();
    }

    [GlobalSetup(Targets = [nameof(DecryptStream_AesSiv512), nameof(DecryptFile_AesSiv512)])]
    public void SetupAesSivDecrypt()
    {
        EnsureGlobalSetup();
        _encryptedSivPath = CreateTempOutputPath();
        _sivService.EncryptFileAsync(_plaintextPath, _encryptedSivPath, key: _sivKey).GetAwaiter().GetResult();
    }

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task EncryptStream_AesGcm()
    {
        await using var input = new DeterministicPayloadStream(DataSize, BenchmarkData.PayloadSeed);
        await using var output = new NullingStream();
        await _aesGcmService.EncryptToStreamAsync(input, output, EncryptionBenchmarkSupport.KeyId, chunkSize: ChunkSize);
    }

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task DecryptStream_AesGcm()
    {
        await using var input = File.OpenRead(_encryptedAesGcmPath);
        await using var output = new NullingStream();
        await _aesGcmService.DecryptToStreamAsync(input, output, EncryptionBenchmarkSupport.KeyId);
    }

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task EncryptFile_AesGcm() => await _aesGcmService.EncryptFileAsync(_plaintextPath, CreateIterationOutputPath(), EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task DecryptFile_AesGcm() => await _aesGcmService.DecryptFileAsync(_encryptedAesGcmPath, CreateIterationOutputPath(), EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task EncryptStream_ChaCha()
    {
        await using var input = new DeterministicPayloadStream(DataSize, BenchmarkData.PayloadSeed);
        await using var output = new NullingStream();
        await _chachaService.EncryptToStreamAsync(input, output, EncryptionBenchmarkSupport.KeyId, chunkSize: ChunkSize);
    }

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task DecryptStream_ChaCha()
    {
        await using var input = File.OpenRead(_encryptedChachaPath);
        await using var output = new NullingStream();
        await _chachaService.DecryptToStreamAsync(input, output, EncryptionBenchmarkSupport.KeyId);
    }

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task EncryptFile_ChaCha() => await _chachaService.EncryptFileAsync(_plaintextPath, CreateIterationOutputPath(), EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task DecryptFile_ChaCha() => await _chachaService.DecryptFileAsync(_encryptedChachaPath, CreateIterationOutputPath(), EncryptionBenchmarkSupport.KeyId);

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task EncryptStream_AesSiv512()
    {
        await using var input = new DeterministicPayloadStream(DataSize, BenchmarkData.PayloadSeed);
        await using var output = new NullingStream();
        await _sivService.EncryptToStreamAsync(input, output, key: _sivKey, chunkSize: ChunkSize);
    }

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task DecryptStream_AesSiv512()
    {
        await using var input = File.OpenRead(_encryptedSivPath);
        await using var output = new NullingStream();
        await _sivService.DecryptToStreamAsync(input, output, key: _sivKey);
    }

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task EncryptFile_AesSiv512() => await _sivService.EncryptFileAsync(_plaintextPath, CreateIterationOutputPath(), key: _sivKey);

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task DecryptFile_AesSiv512() => await _sivService.DecryptFileAsync(_encryptedSivPath, CreateIterationOutputPath(), key: _sivKey);
}