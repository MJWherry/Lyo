using Amazon.S3;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.S3.Staged;
using Lyo.FileStorage.S3.Tests.Support;
using Lyo.FileStorage.Staged;

namespace Lyo.FileStorage.S3.Tests;

/// <summary>Staged upload presign and coordinator integration using the shared <see cref="FakeAmazonS3" /> stub.</summary>
public sealed class S3StagedFileUploadServiceTests : IDisposable
{
    private readonly FakeAmazonS3 _fakeS3;
    private readonly string _metadataRoot;
    private readonly S3FileStorageOptions _options;
    private readonly IAmazonS3 _s3;
    private readonly S3FileStorageService _storage;
    private readonly InMemoryStagedFileUploadStore _store = new();

    public S3StagedFileUploadServiceTests()
    {
        _metadataRoot = Path.Combine(Path.GetTempPath(), "lyo-s3-staged-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_metadataRoot);
        _s3 = FakeAmazonS3.Create(out _fakeS3);
        _options = new() { BucketName = "test-bucket", KeyPrefix = "tenant/files", ServerSideEncryption = "AES256" };
        _storage = new(_options, new LocalFileMetadataStore(_metadataRoot), s3Client: _s3);
    }

    public void Dispose()
    {
        if (_storage is IDisposable d)
            d.Dispose();

        if (Directory.Exists(_metadataRoot))
            Directory.Delete(_metadataRoot, true);
    }

    private S3StagedFileUploadService CreateStaged() => new(_storage, _options, _s3, _store);

    [Fact]
    public async Task BeginAsync_GeneratesPresignedPut_WithStageKeyAndSseHeaders()
    {
        var staged = CreateStaged();
        var begin = await staged.BeginAsync(
            new() {
                DeclaredMaxSizeBytes = 128,
                OriginalFileName = "doc.pdf",
                ContentType = "application/pdf",
                PathPrefix = "inbox"
            }, TestContext.Current.CancellationToken);

        Assert.Equal(MultipartUploadProviderKind.AwsS3, begin.ProviderKind);
        Assert.Contains("https://s3.test/", begin.PresignedPutUrl, StringComparison.Ordinal);
        Assert.Single(_fakeS3.PreSignedUrlRequests);
        var presign = _fakeS3.PreSignedUrlRequests[0];
        Assert.Equal("test-bucket", presign.BucketName);
        Assert.Equal(HttpVerb.PUT, presign.Verb);
        Assert.Equal("application/pdf", presign.ContentType);
        Assert.Equal(ServerSideEncryptionMethod.AES256, presign.ServerSideEncryptionMethod);
        Assert.Contains("tenant/files/inbox/.stage/", begin.StorageLocation, StringComparison.Ordinal);
        Assert.EndsWith("/object", begin.StorageLocation, StringComparison.Ordinal);
        Assert.NotNull(begin.RequiredPutHeaders);
        Assert.Equal("AES256", begin.RequiredPutHeaders!["x-amz-server-side-encryption"]);
        Assert.Equal("application/pdf", begin.RequiredPutHeaders!["Content-Type"]);
        var persisted = await _store.GetAsync(begin.StageId, TestContext.Current.CancellationToken);
        Assert.NotNull(persisted);
        Assert.Equal(begin.StorageLocation, persisted.StorageLocation);
        Assert.Equal(StagedUploadStatus.PendingUpload, persisted.Status);
    }

    [Fact]
    public async Task CompleteAsync_WhenObjectMissing_ThrowsFileNotFound()
    {
        var staged = CreateStaged();
        var begin = await staged.BeginAsync(new() { DeclaredMaxSizeBytes = 64, OriginalFileName = "missing.bin" }, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<FileNotFoundException>(() => staged.CompleteAsync(begin.StageId, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CompleteAsync_WhenObjectExists_UpdatesStore()
    {
        var staged = CreateStaged();
        var begin = await staged.BeginAsync(new() { DeclaredMaxSizeBytes = 64, OriginalFileName = "exists.bin" }, TestContext.Current.CancellationToken);
        _fakeS3.OnGetObjectMetadata = _ => new() { ContentLength = 64, Headers = { ContentLength = 64 } };
        _fakeS3.OnGetObject = _ => new() { ResponseStream = new MemoryStream(new byte[64]) };
        var completed = await staged.CompleteAsync(begin.StageId, ct: TestContext.Current.CancellationToken);
        Assert.Equal(StagedUploadStatus.Uploaded, completed.Status);
        Assert.Equal(64, completed.ObservedSizeBytes);
        Assert.True(_fakeS3.GetObjectMetadataRequests.Count >= 2);
        Assert.Equal(begin.StorageLocation, _fakeS3.GetObjectMetadataRequests[^1].Key);
    }

    [Fact]
    public async Task AbortAsync_DeletesStageObject()
    {
        var staged = CreateStaged();
        var begin = await staged.BeginAsync(new() { DeclaredMaxSizeBytes = 32, OriginalFileName = "abort.bin" }, TestContext.Current.CancellationToken);
        _fakeS3.OnGetObjectMetadata = _ => new() { ContentLength = 32, Headers = { ContentLength = 32 } };
        await staged.AbortAsync(begin.StageId, TestContext.Current.CancellationToken);
        Assert.Single(_fakeS3.DeleteObjectRequests);
        Assert.Equal("test-bucket", _fakeS3.DeleteObjectRequests[0].BucketName);
        Assert.Equal(begin.StorageLocation, _fakeS3.DeleteObjectRequests[0].Key);
        var stage = await staged.GetAsync(begin.StageId, TestContext.Current.CancellationToken);
        Assert.Equal(StagedUploadStatus.Aborted, stage.Status);
    }
}