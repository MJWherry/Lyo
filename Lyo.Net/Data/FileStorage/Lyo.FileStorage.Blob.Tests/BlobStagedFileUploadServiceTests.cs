using Azure.Storage.Blobs;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Blob.Staged;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Staged;

namespace Lyo.FileStorage.Blob.Tests;

/// <summary>Staged upload SAS PUT generation via <see cref="BlobStagedFileUploadService" /> (client-side signing; no live Azure call for Begin).</summary>
public sealed class BlobStagedFileUploadServiceTests : IDisposable
{
    // Well-known Azurite/dev storage account key — used only for offline SAS generation in tests.
    private const string TestConnectionString =
        "DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;EndpointSuffix=core.windows.net";

    private readonly BlobContainerClient _container;

    private readonly string _metadataRoot;
    private readonly BlobFileStorageOptions _options;
    private readonly BlobFileStorageService _storage;
    private readonly InMemoryStagedFileUploadStore _store = new();

    public BlobStagedFileUploadServiceTests()
    {
        _metadataRoot = Path.Combine(Path.GetTempPath(), "lyo-blob-staged-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_metadataRoot);
        _options = new() {
            ConnectionString = TestConnectionString,
            ContainerName = "uploads",
            BlobPrefix = "tenant/files",
            EncryptionScope = "test-scope"
        };

        _container = new(_options.ConnectionString, _options.ContainerName);
        _storage = new(_options, new LocalFileMetadataStore(_metadataRoot), containerClient: _container);
    }

    public void Dispose()
    {
        if (_storage is IDisposable d)
            d.Dispose();

        if (Directory.Exists(_metadataRoot))
            Directory.Delete(_metadataRoot, true);
    }

    private BlobStagedFileUploadService CreateStaged() => new(_storage, _options, _store, _container);

    [Fact]
    public async Task BeginAsync_GeneratesSasPut_WithStageKeyAndRequiredHeaders()
    {
        var staged = CreateStaged();
        var begin = await staged.BeginAsync(
            new() {
                DeclaredMaxSizeBytes = 256,
                OriginalFileName = "photo.jpg",
                ContentType = "image/jpeg",
                PathPrefix = "gallery"
            }, TestContext.Current.CancellationToken);

        Assert.Equal(MultipartUploadProviderKind.AzureBlob, begin.ProviderKind);
        Assert.Contains("sig=", begin.PresignedPutUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tenant/files/gallery/.stage/", begin.StorageLocation, StringComparison.Ordinal);
        Assert.EndsWith("/object", begin.StorageLocation, StringComparison.Ordinal);
        Assert.NotNull(begin.RequiredPutHeaders);
        Assert.Equal("BlockBlob", begin.RequiredPutHeaders!["x-ms-blob-type"]);
        Assert.Equal("image/jpeg", begin.RequiredPutHeaders!["Content-Type"]);
        Assert.Equal("test-scope", begin.RequiredPutHeaders!["x-ms-encryption-scope"]);
        var persisted = await _store.GetAsync(begin.StageId, TestContext.Current.CancellationToken);
        Assert.NotNull(persisted);
        Assert.Equal(begin.StorageLocation, persisted.StorageLocation);
        Assert.Equal(StagedUploadStatus.PendingUpload, persisted.Status);
    }

    [Fact]
    public async Task BeginAsync_WithCustomerProvidedKey_ThrowsNotSupported()
    {
        var cpkOptions = new BlobFileStorageOptions {
            ConnectionString = TestConnectionString, ContainerName = "uploads", CustomerProvidedKeyBase64 = Convert.ToBase64String(new byte[32])
        };

        var cpkContainer = new BlobContainerClient(cpkOptions.ConnectionString, cpkOptions.ContainerName);
        using var cpkStorage = new BlobFileStorageService(cpkOptions, new LocalFileMetadataStore(_metadataRoot), containerClient: cpkContainer);
        var staged = new BlobStagedFileUploadService(cpkStorage, cpkOptions, _store, cpkContainer);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => staged.BeginAsync(
            new() { DeclaredMaxSizeBytes = 16, OriginalFileName = "x.bin" }, TestContext.Current.CancellationToken));

        Assert.Contains("SSE-C", ex.Message);
    }
}