using Lyo.Compression;
using Lyo.Compression.Compressors;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.FileStorage.Tests.Support;
using Lyo.KeyStore;
using Lyo.Testing;

namespace Lyo.FileStorage.Tests;

/// <summary>
/// Drives the two-key storage pipelines the same way <see cref="CompressEncryptPipelineTests" /> covers the single-key helper: a failure in the pipe-<em>reading</em> stage
/// must surface as an exception rather than deadlocking the writing stage on pipe backpressure.
/// </summary>
public sealed class FileStorageStreamingPipelinesFailureTests
{
    private const string KeyId = "pipeline-key";

    private static readonly TimeSpan NoHangTimeout = TimeSpan.FromSeconds(30);

    private static CompressionService CreateCompressionService(long? maxInputSize = null)
        => new(
            [new GZipCompressorFactory(), new DeflateCompressorFactory(), new BrotliCompressorFactory(), new ZLibCompressorFactory()],
            options: maxInputSize == null ? new() : new() { MaxInputSize = maxInputSize.Value });

    private static ITwoKeyEncryptionService CreateEncryptionService()
    {
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(KeyId, "1", "pipeline-kek");
        keyStore.SetCurrentVersion(KeyId, "1");
        return new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(new AesGcmEncryptionService(keyStore), keyStore);
    }

    [Fact]
    public async Task SaveFromStreamAsync_CompressEncrypt_EncryptStageFails_Throws_InsteadOfDeadlocking()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = LocalFileStorageTestScope.Create(compressionService: CreateCompressionService(), twoKeyEncryptionService: CreateEncryptionService());

        // The encrypt stage faults on the unknown key before it consumes the pipe, while the compress stage still wants to
        // write ~1 MB — well past the pipe's ~64 KB pause threshold, which is the condition that used to deadlock.
        var payload = TestData.Create(1024 * 1024);
        await using var input = new MemoryStream(payload, false);
        var ex = await Assert.ThrowsAnyAsync<Exception>(()
            => scope.Storage.SaveFromStreamAsync(input, payload.LongLength, "pipeline.bin", true, true, "no-such-key", ct: ct).WaitAsync(NoHangTimeout, ct));

        Assert.IsNotType<TimeoutException>(ex);
    }

    [Fact]
    public async Task GetFileStreamAsync_DecryptDecompress_DecompressStageFails_SurfacesFaultToConsumer()
    {
        var ct = TestContext.Current.CancellationToken;
        var encryption = CreateEncryptionService();

        // Incompressible payload so the compressed body stays large enough for the read-side guard to fire mid-stream.
        var payload = TestData.Create(1024 * 1024);
        using var writeScope = LocalFileStorageTestScope.Create(compressionService: CreateCompressionService(), twoKeyEncryptionService: encryption);
        await using var input = new MemoryStream(payload, false);
        var saved = await writeScope.Storage.SaveFromStreamAsync(input, payload.LongLength, "pipeline.bin", true, true, KeyId, ct: ct);
        Assert.True(saved.IsCompressed);
        Assert.True(saved.IsEncrypted);

        // Second service over the same root, but its decompressor is capped at 1 KB (the options minimum) so the guard fires
        // inside the pipe-reading stage. The fault has to reach the caller through PipelineFileReadStream rather than sit
        // stranded on the background pipeline task.
        using var readService = new LocalFileStorageService(writeScope.Options, compressionService: CreateCompressionService(1024), twoKeyEncryptionService: encryption);
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () => {
            await using var stream = await readService.GetFileStreamAsync(saved.Id, ct: ct).WaitAsync(NoHangTimeout, ct);
            await using var sink = new MemoryStream();
            await stream!.CopyToAsync(sink, ct).WaitAsync(NoHangTimeout, ct);
        });

        Assert.IsNotType<TimeoutException>(ex);
    }
}
