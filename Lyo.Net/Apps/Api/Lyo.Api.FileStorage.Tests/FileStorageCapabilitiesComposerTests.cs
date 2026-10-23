using Lyo.Common.Core.Identifiers;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage;
using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;
using Lyo.KeyStore;

namespace Lyo.Api.FileStorage.Tests;

public sealed class FileStorageCapabilitiesComposerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lyo-fs-api-caps-" + LyoGuid.CreateV7().ToString("N"));

    public FileStorageCapabilitiesComposerTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    [Fact]
    public void Compose_LocalMultipartAndNoKeyStore_PresignedPartsAndEncryptionFalse()
    {
        var options = new DiskFileStorageOptions { RootDirectoryPath = _root };
        using var storage = new LocalFileStorageService(options);
        var multipart = new LocalMultipartUploadService(storage, new InMemoryMultipartUploadSessionStore(), options);
        var composed = FileStorageCapabilitiesComposer.Compose(storage, multipart, false, true, true);
        Assert.True(composed.Multipart);
        Assert.False(composed.PresignedMultipartParts);
        Assert.False(composed.Encryption);
        Assert.False(composed.PresignedRead);
        Assert.False(composed.DirectUpload);
        Assert.True(composed.Copy);
        Assert.True(composed.Move);
        Assert.True(composed.Physical);
        Assert.True(composed.Archive);
        Assert.True(composed.AccessLinks);
        Assert.False(FileStorageCapabilitiesComposer.HasEncryptionStore(null, null, null));
    }

    [Fact]
    public void Compose_PresignedMultipartAndKeyStore_PresignedPartsAndEncryptionTrue()
    {
        var options = new DiskFileStorageOptions { RootDirectoryPath = _root };
        using var storage = new LocalFileStorageService(options);
        var keys = new LocalKeyStore();
        var composed = FileStorageCapabilitiesComposer.Compose(storage, new PresignedMultipart(), true, false, false);
        Assert.True(composed.Multipart);
        Assert.True(composed.PresignedMultipartParts);
        Assert.True(composed.Encryption);
        Assert.False(composed.Archive);
        Assert.False(composed.AccessLinks);
        Assert.True(FileStorageCapabilitiesComposer.HasEncryptionStore(keys, null, null));
        Assert.True(FileStorageCapabilitiesComposer.HasEncryptionStore(null, keys, null));
        Assert.True(FileStorageCapabilitiesComposer.HasEncryptionStore(null, null, keys));
    }

    private sealed class PresignedMultipart : IMultipartUploadService
    {
        public Task<MultipartBeginResult> BeginAsync(MultipartBeginRequest request, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<MultipartPartDescriptor> GetPresignedPartUploadAsync(Guid sessionId, int partNumber, CancellationToken ct = default) => throw new NotSupportedException();

        public Task UploadPartAsync(Guid sessionId, int partNumber, Stream content, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<FileStoreResult> CompleteAsync(CompleteMultipartUploadRequest request, CancellationToken ct = default) => throw new NotSupportedException();

        public Task AbortAsync(Guid sessionId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}