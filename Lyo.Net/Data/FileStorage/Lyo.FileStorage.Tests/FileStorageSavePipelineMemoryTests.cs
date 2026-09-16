using System.Security.Cryptography;
using Lyo.Compression;
using Lyo.Compression.Compressors;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Tests.Support;
using Lyo.KeyStore;
using Lyo.Streams;
using Lyo.Testing;

namespace Lyo.FileStorage.Tests;

/// <summary>
/// Saves that cannot hit duplicate detection must stream straight to the backend. The staging buffer only exists so the plaintext hash is known before the object is
/// committed, which is a duplicate-detection requirement, so with dedup off it is a second full copy of the payload for nothing.
/// </summary>
[Collection(SerialAllocationCollection.Name)]
public sealed class FileStorageSavePipelineMemoryTests
{
    private const int PayloadBytes = 8 * 1024 * 1024;

    private static CompressionService CreateCompressionService()
        => new([new GZipCompressorFactory(), new DeflateCompressorFactory(), new BrotliCompressorFactory(), new ZLibCompressorFactory()]);

    private static ITwoKeyEncryptionService CreateEncryptionService(string keyId)
    {
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(keyId, "1", "test-kek-key-material-32bytes!!");
        keyStore.SetCurrentVersion(keyId, "1");
        return new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(new AesGcmEncryptionService(keyStore), keyStore);
    }

    [Fact]
    public async Task SaveFromStreamAsync_PlainWithDedupOff_AllocatesWellUnderPayloadSize()
    {
        using var scope = LocalFileStorageTestScope.Create();

        // Warm the pipeline so first-call JIT and static initialisation stay out of the measurement.
        using (var warmup = new SpanReadStream(new DeterministicPayloadStream(1024)))
            await scope.Storage.SaveFromStreamAsync(warmup, 1024, "warmup.bin", ct: TestCt);

        long allocated;
        using (var payload = new SpanReadStream(new DeterministicPayloadStream(PayloadBytes))) {
            var before = GC.GetTotalAllocatedBytes(true);
            await scope.Storage.SaveFromStreamAsync(payload, PayloadBytes, "large.bin", ct: TestCt);
            allocated = GC.GetTotalAllocatedBytes(true) - before;
        }

        Assert.True(
            allocated < 2 * 1024 * 1024,
            $"Plain save of {PayloadBytes:N0} bytes allocated {allocated:N0} bytes; the payload should stream to the backend rather than being staged in full.");
    }

    [Fact]
    public async Task SaveFromStreamAsync_DeclaredLengthFarExceedsActual_DoesNotPreallocateDeclaredSize()
    {
        using var scope = LocalFileStorageTestScope.Create(
            o => {
                o.EnableDuplicateDetection = true;
                return o;
            });

        // Warm the pipeline so first-call JIT and static initialisation stay out of the measurement.
        using (var warmup = new MemoryStream(TestData.Create(16), false))
            await scope.Storage.SaveFromStreamAsync(warmup, 16, "warmup.bin", ct: TestCt);

        const long declaredSize = 64L * 1024 * 1024;
        var payload = TestData.Create(10);

        long allocated;
        FileStoreResult saved;
        using (var input = new MemoryStream(payload, false)) {
            var before = GC.GetTotalAllocatedBytes(true);
            saved = await scope.Storage.SaveFromStreamAsync(input, declaredSize, "overstated.bin", ct: TestCt);
            allocated = GC.GetTotalAllocatedBytes(true) - before;
        }

        Assert.Equal(payload.LongLength, saved.OriginalFileSize);
        Assert.Equal(payload, await scope.Storage.GetFileAsync(saved.Id, ct: TestCt));
        Assert.True(
            allocated < 2 * 1024 * 1024,
            $"A {payload.Length}-byte save that declared {declaredSize:N0} bytes allocated {allocated:N0} bytes; a caller-supplied length must not scale staging preallocation.");
    }

    [Fact]
    public async Task SaveFromStreamAsync_CompressOnlyWithDedupOff_RecordsCompressedSizeAndHash()
    {
        using var scope = LocalFileStorageTestScope.Create(compressionService: CreateCompressionService());
        var payload = TestData.Create(256 * 1024);
        var expectedOriginalHash = SHA256.HashData(payload);

        using var input = new MemoryStream(payload, false);
        var saved = await scope.Storage.SaveFromStreamAsync(input, payload.LongLength, "compress-only.bin", true, ct: TestCt);

        Assert.True(saved.IsCompressed);
        Assert.False(saved.IsEncrypted);
        Assert.Equal(expectedOriginalHash, saved.OriginalFileHash);
        Assert.Equal(payload.LongLength, saved.OriginalFileSize);
        Assert.Equal(saved.CompressedFileSize, saved.SourceFileSize);
        Assert.Equal(saved.CompressedFileHash, saved.SourceFileHash);

        // CompressedFileSize used to be read off the staging stream; it has to keep matching what actually landed in storage.
        Assert.Equal(await StoredLengthAsync(scope, saved), saved.CompressedFileSize);
        Assert.Equal(payload, await scope.Storage.GetFileAsync(saved.Id, ct: TestCt));
    }

    [Fact]
    public async Task SaveFromStreamAsync_EncryptOnlyWithDedupOff_RoundTripsAndRecordsEncryptedSize()
    {
        using var scope = LocalFileStorageTestScope.Create(twoKeyEncryptionService: CreateEncryptionService("enc-key"));
        var payload = TestData.Create(256 * 1024);
        var expectedOriginalHash = SHA256.HashData(payload);

        using var input = new MemoryStream(payload, false);
        var saved = await scope.Storage.SaveFromStreamAsync(input, payload.LongLength, "encrypt-only.bin", encrypt: true, keyId: "enc-key", ct: TestCt);

        Assert.True(saved.IsEncrypted);
        Assert.False(saved.IsCompressed);
        Assert.Equal(expectedOriginalHash, saved.OriginalFileHash);
        Assert.Equal(payload.LongLength, saved.OriginalFileSize);
        Assert.Equal(saved.EncryptedFileSize, saved.SourceFileSize);
        Assert.Equal(saved.EncryptedFileHash, saved.SourceFileHash);
        Assert.Equal("enc-key", saved.DataEncryptionKeyId);
        Assert.Equal(await StoredLengthAsync(scope, saved), saved.EncryptedFileSize);
        Assert.Equal(payload, await scope.Storage.GetFileAsync(saved.Id, ct: TestCt));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task SaveFromStreamAsync_NonSeekableSourceWithDedupOff_RoundTrips(bool compress, bool encrypt)
    {
        using var scope = LocalFileStorageTestScope.Create(
            compressionService: CreateCompressionService(), twoKeyEncryptionService: CreateEncryptionService("enc-key"));

        var payload = TestData.Create(128 * 1024);
        using var input = new NonSeekableReadStream(new MemoryStream(payload, false));
        var saved = await scope.Storage.SaveFromStreamAsync(
            input, payload.LongLength, "non-seekable.bin", compress, encrypt, encrypt ? "enc-key" : null, ct: TestCt);

        Assert.Equal(payload.LongLength, saved.OriginalFileSize);
        Assert.Equal(SHA256.HashData(payload), saved.OriginalFileHash);
        Assert.Equal(payload, await scope.Storage.GetFileAsync(saved.Id, ct: TestCt));
    }

    private static async Task<long> StoredLengthAsync(LocalFileStorageTestScope scope, FileStoreResult saved)
    {
        var key = CloudObjectKeyBuilder.FromMetadata(saved.Id, saved.SourceFileName, saved.PathPrefix);
        await using var stored = await scope.Storage.Physical.OpenReadAsync(Path.Combine(scope.Options.RootDirectoryPath, key), TestCt);
        return stored.Length;
    }
}
