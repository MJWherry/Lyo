using Lyo.Compression;
using Lyo.Compression.Compressors;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Tests.Support;
using Lyo.KeyStore;

namespace Lyo.FileStorage.Tests;

/// <summary>Duplicate detection must honor the compress/encrypt storage profile for ReturnExisting; Overwrite must apply the new profile.</summary>
public sealed class LocalFileStorageDuplicateProfileTests
{
    private static ICompressorFactory[] BuiltInCompressorFactories()
        => [
            new GZipCompressorFactory(), new DeflateCompressorFactory(),
#if !NETSTANDARD2_0
            new BrotliCompressorFactory(), new ZLibCompressorFactory(),
#endif
        ];

    private static CompressionService CreateCompressionService() => new(BuiltInCompressorFactories());

    private static ITwoKeyEncryptionService CreateEncryptionService(string keyId, string keyString = "test-kek-key-material-32b!")
    {
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(keyId, "1", keyString);
        keyStore.SetCurrentVersion(keyId, "1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        return new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
    }

    private static LocalFileStorageTestScope CreateDedupScope(
        DuplicateHandlingStrategy strategy,
        ICompressionService? compressionService = null,
        ITwoKeyEncryptionService? twoKeyEncryptionService = null)
        => LocalFileStorageTestScope.Create(
            o => {
                o.EnableDuplicateDetection = true;
                o.DuplicateStrategy = strategy;
                return o;
            }, compressionService: compressionService, twoKeyEncryptionService: twoKeyEncryptionService);

    [Fact]
    public async Task ReturnExisting_SameProfile_ReusesExistingFileId()
    {
        using var scope = CreateDedupScope(DuplicateHandlingStrategy.ReturnExisting);
        var payload = "dedup-same-profile"u8.ToArray();
        var first = await scope.Storage.SaveFileAsync(payload, "a.txt", ct: TestCt);
        var second = await scope.Storage.SaveFileAsync(payload, "b.txt", ct: TestCt);
        Assert.Equal(first.Id, second.Id);
        Assert.False(second.IsCompressed);
        Assert.False(second.IsEncrypted);
    }

    [Fact]
    public async Task ReturnExisting_PlainThenCompress_ThrowsConflict()
    {
        using var scope = CreateDedupScope(DuplicateHandlingStrategy.ReturnExisting, CreateCompressionService());
        var payload = "plain-then-compress"u8.ToArray();
        await scope.Storage.SaveFileAsync(payload, "plain.txt", ct: TestCt);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => scope.Storage.SaveFileAsync(payload, "compressed.txt", true, ct: TestCt));
        Assert.Contains("storage profile", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReturnExisting_CompressThenPlain_ThrowsConflict()
    {
        using var scope = CreateDedupScope(DuplicateHandlingStrategy.ReturnExisting, CreateCompressionService());
        var payload = "compress-then-plain"u8.ToArray();
        await scope.Storage.SaveFileAsync(payload, "compressed.txt", true, ct: TestCt);
        await Assert.ThrowsAsync<ConflictException>(() => scope.Storage.SaveFileAsync(payload, "plain.txt", ct: TestCt));
    }

    [Fact]
    public async Task ReturnExisting_EncryptedDifferentKeyId_ThrowsConflict()
    {
        using var scope = LocalFileStorageTestScope.Create(
            o => {
                o.EnableDuplicateDetection = true;
                o.DuplicateStrategy = DuplicateHandlingStrategy.ReturnExisting;
                return o;
            }, twoKeyEncryptionService: CreateEncryptionServiceForTwoKeys("key-a", "key-b"));

        var payload = "key-mismatch"u8.ToArray();
        await scope.Storage.SaveFileAsync(payload, "first.txt", encrypt: true, keyId: "key-a", ct: TestCt);
        await Assert.ThrowsAsync<ConflictException>(() => scope.Storage.SaveFileAsync(payload, "second.txt", encrypt: true, keyId: "key-b", ct: TestCt));
    }

    [Fact]
    public async Task ReturnExisting_AfterDelete_AllowsReuploadWithDifferentProfile()
    {
        using var scope = CreateDedupScope(DuplicateHandlingStrategy.ReturnExisting, CreateCompressionService());
        var payload = "delete-then-plain"u8.ToArray();
        var compressed = await scope.Storage.SaveFileAsync(payload, "compressed.txt", true, ct: TestCt);
        Assert.True(await scope.Storage.DeleteFileAsync(compressed.Id, ct: TestCt));
        var plain = await scope.Storage.SaveFileAsync(payload, "plain.txt", ct: TestCt);
        Assert.NotEqual(compressed.Id, plain.Id);
        Assert.False(plain.IsCompressed);
        Assert.Equal(payload, await scope.Storage.GetFileAsync(plain.Id, ct: TestCt));
    }

    [Fact]
    public async Task AllowDuplicate_DifferentProfile_AllocatesSecondFileId()
    {
        using var scope = CreateDedupScope(DuplicateHandlingStrategy.AllowDuplicate, CreateCompressionService());
        var payload = "allow-dup-profile"u8.ToArray();
        var plain = await scope.Storage.SaveFileAsync(payload, "plain.txt", ct: TestCt);
        var compressed = await scope.Storage.SaveFileAsync(payload, "compressed.txt", true, ct: TestCt);
        Assert.NotEqual(plain.Id, compressed.Id);
        Assert.True(compressed.IsCompressed);
    }

    [Fact]
    public async Task Overwrite_PlainThenCompress_SameIdWithNewProfile()
    {
        using var scope = CreateDedupScope(DuplicateHandlingStrategy.Overwrite, CreateCompressionService());
        var payload = "overwrite-to-compress"u8.ToArray();
        var plain = await scope.Storage.SaveFileAsync(payload, "plain.txt", ct: TestCt);
        var compressed = await scope.Storage.SaveFileAsync(payload, "compressed.txt", true, ct: TestCt);
        Assert.Equal(plain.Id, compressed.Id);
        Assert.True(compressed.IsCompressed);
        Assert.False(compressed.IsEncrypted);
        var roundtrip = await scope.Storage.GetFileAsync(compressed.Id, ct: TestCt);
        Assert.Equal(payload, roundtrip);
    }

    [Fact]
    public async Task Overwrite_CompressThenPlain_SameIdWithNewProfile()
    {
        using var scope = CreateDedupScope(DuplicateHandlingStrategy.Overwrite, CreateCompressionService());
        var payload = "overwrite-to-plain"u8.ToArray();
        var compressed = await scope.Storage.SaveFileAsync(payload, "compressed.txt", true, ct: TestCt);
        var plain = await scope.Storage.SaveFileAsync(payload, "plain.txt", ct: TestCt);
        Assert.Equal(compressed.Id, plain.Id);
        Assert.False(plain.IsCompressed);
        var roundtrip = await scope.Storage.GetFileAsync(plain.Id, ct: TestCt);
        Assert.Equal(payload, roundtrip);
    }

    [Fact]
    public async Task Overwrite_CompressedDuplicate_SourceFileNameMatchesSurvivingId()
    {
        using var scope = CreateDedupScope(DuplicateHandlingStrategy.Overwrite, CreateCompressionService());
        var payload = "overwrite-source-name-compressed"u8.ToArray();
        await scope.Storage.SaveFileAsync(payload, "plain.txt", ct: TestCt);
        var compressed = await scope.Storage.SaveFileAsync(payload, "compressed.txt", true, ct: TestCt);

        // The cloud backends locate the object via CloudObjectKeyBuilder.FromMetadata, so a SourceFileName carrying the
        // discarded upload id silently drops the extension and the read 404s on S3 and Blob.
        Assert.Equal(compressed.CompressionAlgorithm!.Extension, CloudObjectKeyBuilder.InferTrailingSuffixAfterFileId(compressed.Id, compressed.SourceFileName));
    }

    [Fact]
    public async Task Overwrite_PlainDuplicate_SourceFileNameMatchesSurvivingId()
    {
        using var scope = CreateDedupScope(DuplicateHandlingStrategy.Overwrite, CreateCompressionService());
        var payload = "overwrite-source-name-plain"u8.ToArray();
        await scope.Storage.SaveFileAsync(payload, "compressed.txt", true, ct: TestCt);
        var plain = await scope.Storage.SaveFileAsync(payload, "plain.txt", ct: TestCt);
        Assert.StartsWith(plain.Id.ToString(), plain.SourceFileName, StringComparison.Ordinal);
        Assert.Equal("", CloudObjectKeyBuilder.InferTrailingSuffixAfterFileId(plain.Id, plain.SourceFileName));
    }

    [Fact]
    public async Task Overwrite_EncryptedKeyChange_UpdatesDataEncryptionKeyId()
    {
        using var scope = LocalFileStorageTestScope.Create(
            o => {
                o.EnableDuplicateDetection = true;
                o.DuplicateStrategy = DuplicateHandlingStrategy.Overwrite;
                return o;
            }, twoKeyEncryptionService: CreateEncryptionServiceForTwoKeys("key-a", "key-b"));

        var payload = "overwrite-key"u8.ToArray();
        var first = await scope.Storage.SaveFileAsync(payload, "first.txt", encrypt: true, keyId: "key-a", ct: TestCt);
        var second = await scope.Storage.SaveFileAsync(payload, "second.txt", encrypt: true, keyId: "key-b", ct: TestCt);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal("key-b", second.DataEncryptionKeyId);
        var roundtrip = await scope.Storage.GetFileAsync(second.Id, ct: TestCt);
        Assert.Equal(payload, roundtrip);
    }

    [Fact]
    public async Task Overwrite_PlainThenEncrypt_SameIdWithEncryptedMetadata()
    {
        using var scope = CreateDedupScope(DuplicateHandlingStrategy.Overwrite, twoKeyEncryptionService: CreateEncryptionService("enc-key"));
        var payload = "plain-to-encrypt"u8.ToArray();
        var plain = await scope.Storage.SaveFileAsync(payload, "plain.txt", ct: TestCt);
        var encrypted = await scope.Storage.SaveFileAsync(payload, "encrypted.txt", encrypt: true, keyId: "enc-key", ct: TestCt);
        Assert.Equal(plain.Id, encrypted.Id);
        Assert.True(encrypted.IsEncrypted);
        Assert.Equal("enc-key", encrypted.DataEncryptionKeyId);
        var roundtrip = await scope.Storage.GetFileAsync(encrypted.Id, ct: TestCt);
        Assert.Equal(payload, roundtrip);
    }

    private static ITwoKeyEncryptionService CreateEncryptionServiceForTwoKeys(string keyIdA, string keyIdB)
    {
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(keyIdA, "1", "test-kek-key-material-32bytes!!");
        keyStore.SetCurrentVersion(keyIdA, "1");
        keyStore.AddKeyFromString(keyIdB, "1", "other-kek-key-material-32bytes!");
        keyStore.SetCurrentVersion(keyIdB, "1");
        var aesGcmService = new AesGcmEncryptionService(keyStore);
        return new TwoKeyEncryptionService<IEncryptionService, IEncryptionService>(aesGcmService, keyStore);
    }
}