using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.FileStorage.Models;
using Lyo.KeyStore;
using Lyo.Streams;

namespace Lyo.FileStorage.Benchmarks;

/// <summary>Local disk file storage save/get/delete across sizes and compress/encrypt combinations.</summary>
[BenchmarkDescription(
    "LocalFileStorageService under an IOTemp session directory: SaveFromStream / GetFileStream / DeleteFile for " +
    "DeterministicPayloadStream inputs (BenchmarkData.PayloadSeed, 1 KiB–100 MiB) crossed with compress and encrypt flags. " +
    "Get drains into NullingStream so MemoryDiagnoser excludes retained plaintext. Compression and two-key AES-GCM are " +
    "always wired; flags select the pipeline. SHA-256 hashing and JSON LocalFileMetadataStore are always on.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Plaintext payload size: 1 KiB, 64 KiB, 1 MiB, 10 MiB, 100 MiB.")]
[BenchmarkParameter("Compress", Description = "When true, save/get run through Lyo.Compression (GZip).")]
[BenchmarkParameter("Encrypt", Description = "When true, save/get run through two-key AES-GCM encryption.")]
public class LocalFileStorageBenchmarks : LyoBenchmarkBase
{
    private Guid _getFileId;
    private Guid? _iterationFileId;
    private LocalKeyStore _keyStore = null!;
    private LocalFileStorageService _storage = null!;

    [Params(1024, 65536, BenchmarkData.MiB, 10 * BenchmarkData.MiB, 100 * BenchmarkData.MiB)]
    public int DataSize { get; set; }

    [Params(false, true)]
    public bool Compress { get; set; }

    [Params(false, true)]
    public bool Encrypt { get; set; }

    /// <inheritdoc />
    protected override void OnGlobalSetup()
    {
        var storageRoot = Temp.CreateDirectory("filestorage");
        _keyStore = FileStorageBenchmarkSupport.CreateKeyStore();
        _storage = new LocalFileStorageService(
            new DiskFileStorageOptions { RootDirectoryPath = storageRoot, EnableMetrics = false },
            compressionService: FileStorageBenchmarkSupport.CreateCompressionService(),
            twoKeyEncryptionService: FileStorageBenchmarkSupport.CreateTwoKeyEncryptionService(_keyStore));

        _getFileId = SavePayloadAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    protected override void OnGlobalCleanup() => _storage?.Dispose();

    [Benchmark]
    [BenchmarkDescription("SaveFromStreamAsync from a DeterministicPayloadStream with the current compress/encrypt flags.")]
    public async Task Save_FromStream() => _iterationFileId = await SavePayloadAsync();

    [IterationCleanup(Target = nameof(Save_FromStream))]
    public void CleanupSave()
    {
        if (_iterationFileId is not { } id)
            return;

        _storage.DeleteFileAsync(id).GetAwaiter().GetResult();
        _iterationFileId = null;
    }

    [Benchmark]
    [BenchmarkDescription("GetFileStreamAsync for a file pre-saved with the current compress/encrypt flags; drains into NullingStream.")]
    public async Task Get_Stream()
    {
        await using var stream = await _storage.GetFileStreamAsync(_getFileId);
        if (stream == null)
            return;

        await using var sink = new NullingStream();
        await stream.CopyToAsync(sink);
    }

    [IterationSetup(Target = nameof(Delete))]
    public void SetupDelete() => _iterationFileId = SavePayloadAsync().GetAwaiter().GetResult();

    [Benchmark]
    [BenchmarkDescription("DeleteFileAsync for a file saved in IterationSetup with the current compress/encrypt flags.")]
    public async Task Delete()
    {
        var id = _iterationFileId ?? throw new InvalidOperationException("Delete iteration file was not set up.");
        await _storage.DeleteFileAsync(id);
        _iterationFileId = null;
    }

    private async Task<Guid> SavePayloadAsync()
    {
        await using var input = new DeterministicPayloadStream(DataSize, BenchmarkData.PayloadSeed);
        var result = await _storage.SaveFromStreamAsync(
            input,
            DataSize,
            originalFileName: "bench.bin",
            compress: Compress,
            encrypt: Encrypt,
            keyId: Encrypt ? FileStorageBenchmarkSupport.KeyId : null);
        return result.Id;
    }
}
