using Lyo.Common.Core.Identifiers;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Models;
using Lyo.Health;

namespace Lyo.FileStorage.Tests;

public sealed class FileStorageCapabilitiesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lyo-fs-caps-" + LyoGuid.CreateV7().ToString("N"));

    public FileStorageCapabilitiesTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    [Fact]
    public void GetCapabilities_LocalWithoutFileUriOrDirectUpload_ReportsCopyMoveAndPhysicalOnly()
    {
        using var service = new LocalFileStorageService(new() { RootDirectoryPath = _root });
        var caps = service.GetCapabilities();
        Assert.False(caps.PresignedRead);
        Assert.False(caps.DirectUpload);
        Assert.True(caps.Copy);
        Assert.True(caps.Move);
        Assert.True(caps.Physical);
    }

    [Fact]
    public void GetCapabilities_LocalWithFileUriAndDirectUpload_ReportsPresignedReadAndDirectUpload()
    {
        using var service = new LocalFileStorageService(new() { RootDirectoryPath = _root, AllowFileUriPresignedUrls = true, DirectUploadReceiveBaseUri = "http://localhost:9" });
        var caps = service.GetCapabilities();
        Assert.True(caps.PresignedRead);
        Assert.True(caps.DirectUpload);
        Assert.True(caps.Copy);
        Assert.True(caps.Move);
        Assert.True(caps.Physical);
    }

    [Fact]
    public void GetCapabilities_Base_ReportsPresignDirectCopyAndMoveFalse()
    {
        using var service = new BareStorage(new() { RootDirectoryPath = _root });
        var caps = service.GetCapabilities();
        Assert.False(caps.PresignedRead);
        Assert.False(caps.DirectUpload);
        Assert.False(caps.Copy);
        Assert.False(caps.Move);
        Assert.False(caps.Physical);
    }

    private sealed class BareStorage : FileStorageServiceBase
    {
        public BareStorage(DiskFileStorageOptions options)
            : base(options, new LocalFileMetadataStore(options.RootDirectoryPath)) { }

        protected override Task<HealthResult> CheckHealthLightweightAsync(CancellationToken ct) => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero));

        protected override Task<Stream> CreateOutputStreamAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct) => throw new NotSupportedException();

        protected override Task<long> GetStorageSizeAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct) => throw new NotSupportedException();

        protected override Task<Stream?> ReadFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct) => throw new NotSupportedException();

        protected override Task<bool> DeleteFromStorageAsync(Guid fileId, string? pathPrefix, CancellationToken ct) => throw new NotSupportedException();

        protected override Task<EncryptionHeaderInfo> ExtractEncryptionHeaderAsync(Guid fileId, string extension, string? pathPrefix, CancellationToken ct)
            => throw new NotSupportedException();

        protected override Task UpdateFileHeaderAsync(Guid fileId, string? pathPrefix, string targetKeyId, string targetKeyVersion, byte[] newEncryptedDek, CancellationToken ct)
            => throw new NotSupportedException();

        protected override Task CleanupPartialFileAsync(Guid fileId, string? pathPrefix, CancellationToken ct) => Task.CompletedTask;
    }
}