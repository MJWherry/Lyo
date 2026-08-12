using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.ChaCha20Poly1305;
using Lyo.Encryption.TwoKey;
using Lyo.KeyStore;
using Lyo.Streams;

namespace Lyo.Encryption.Benchmarks;

[BenchmarkDescription(
    "Envelope (two-key DEK/KEK) encrypt/decrypt at 100 MiB–2 GiB with AES-GCM and ChaCha20-Poly1305. Stream methods use DeterministicPayloadStream + NullingStream; file methods use IOTemp paths (EncryptToFileAsync / DecryptToStreamAsync to file).")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Plaintext size: 100, 250, 500, 750 MiB, 1 GiB, 1.5 GiB, 2 GiB.")]
public class TwoKeyEncryptionBenchmarks : LyoBenchmarkBase
{
    private TwoKeyEncryptionService<AesGcmEncryptionService, AesGcmEncryptionService> _aesGcmService = null!;
    private TwoKeyEncryptionService<ChaCha20Poly1305EncryptionService, ChaCha20Poly1305EncryptionService> _chachaService = null!;
    private string _encryptedAesGcmPath = null!;
    private string _encryptedChachaPath = null!;
    private LocalKeyStore _keyStore = null!;
    private string _plaintextPath = null!;

    [Params(
        BenchmarkData.StreamingSize100MiB, BenchmarkData.StreamingSize250MiB, BenchmarkData.StreamingSize500MiB, BenchmarkData.StreamingSize750MiB, BenchmarkData.StreamingSize1GiB,
        BenchmarkData.StreamingSize15GiB, BenchmarkData.StreamingSize2GiB)]
    public long DataSize { get; set; }

    /// <inheritdoc />
    protected override void OnGlobalSetup()
    {
        _keyStore = EncryptionBenchmarkSupport.CreateKeyStore();
        var aesGcmDek = new AesGcmEncryptionService(_keyStore);
        var aesGcmKek = new AesGcmEncryptionService(_keyStore);
        _aesGcmService = new(aesGcmDek, aesGcmKek, _keyStore);
        var chachaDek = new ChaCha20Poly1305EncryptionService(_keyStore);
        var chachaKek = new ChaCha20Poly1305EncryptionService(_keyStore);
        _chachaService = new(chachaDek, chachaKek, _keyStore);
        _plaintextPath = CreateSeededFilePath(DataSize);
    }

    [GlobalSetup(Targets = [nameof(DecryptStream_AesGcm), nameof(DecryptFile_AesGcm)])]
    public void SetupAesGcmDecrypt()
    {
        EnsureGlobalSetup();
        _encryptedAesGcmPath = CreateTempOutputPath();
        using var input = File.OpenRead(_plaintextPath);
        _aesGcmService.EncryptToFileAsync(input, _encryptedAesGcmPath, EncryptionBenchmarkSupport.KeyId).GetAwaiter().GetResult();
    }

    [GlobalSetup(Targets = [nameof(DecryptStream_ChaCha), nameof(DecryptFile_ChaCha)])]
    public void SetupChaChaDecrypt()
    {
        EnsureGlobalSetup();
        _encryptedChachaPath = CreateTempOutputPath();
        using var input = File.OpenRead(_plaintextPath);
        _chachaService.EncryptToFileAsync(input, _encryptedChachaPath, EncryptionBenchmarkSupport.KeyId).GetAwaiter().GetResult();
    }

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task EncryptStream_AesGcm()
    {
        await using var input = new DeterministicPayloadStream(DataSize, BenchmarkData.PayloadSeed);
        await using var output = new NullingStream();
        await _aesGcmService.EncryptToStreamAsync(input, output, EncryptionBenchmarkSupport.KeyId);
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
    public async Task EncryptFile_AesGcm()
    {
        await using var input = File.OpenRead(_plaintextPath);
        await _aesGcmService.EncryptToFileAsync(input, CreateIterationOutputPath(), EncryptionBenchmarkSupport.KeyId);
    }

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task DecryptFile_AesGcm()
    {
        await using var input = File.OpenRead(_encryptedAesGcmPath);
        await using var output = File.Create(CreateIterationOutputPath());
        await _aesGcmService.DecryptToStreamAsync(input, output, EncryptionBenchmarkSupport.KeyId);
    }

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task EncryptStream_ChaCha()
    {
        await using var input = new DeterministicPayloadStream(DataSize, BenchmarkData.PayloadSeed);
        await using var output = new NullingStream();
        await _chachaService.EncryptToStreamAsync(input, output, EncryptionBenchmarkSupport.KeyId);
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
    public async Task EncryptFile_ChaCha()
    {
        await using var input = File.OpenRead(_plaintextPath);
        await _chachaService.EncryptToFileAsync(input, CreateIterationOutputPath(), EncryptionBenchmarkSupport.KeyId);
    }

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task DecryptFile_ChaCha()
    {
        await using var input = File.OpenRead(_encryptedChachaPath);
        await using var output = File.Create(CreateIterationOutputPath());
        await _chachaService.DecryptToStreamAsync(input, output, EncryptionBenchmarkSupport.KeyId);
    }
}