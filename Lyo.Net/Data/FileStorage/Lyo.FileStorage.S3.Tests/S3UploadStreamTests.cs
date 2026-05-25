using Amazon.S3.Model;
using Lyo.FileStorage.S3;
using Lyo.FileStorage.S3.Tests.Support;

namespace Lyo.FileStorage.S3.Tests;

/// <summary>End-to-end behavior of <see cref="S3UploadStream" /> using a hand-rolled <see cref="IAmazonS3" /> stub so we can assert
/// which upload path was selected (single PUT vs multipart), how the content-type propagated, and that aborts run when a part fails.</summary>
public sealed class S3UploadStreamTests
{
    private static S3FileStorageOptions Options(string? sse = null, string? key = null) =>
        new() { BucketName = "test-bucket", ServerSideEncryption = sse, ServerSideEncryptionAwsKmsKeyId = key };

    [Fact]
    public async Task SmallPayload_UsesSinglePut_AndPropagatesContentType()
    {
        var client = FakeAmazonS3.Create(out var fake);
        var stream = new S3UploadStream(client, "test-bucket", "k.bin", Options(), CancellationToken.None);
        stream.SetContentType("application/octet-stream");
        await stream.WriteAsync(new byte[1024], 0, 1024);
        await stream.DisposeAsync();

        Assert.Single(fake.PutObjectRequests);
        var put = fake.PutObjectRequests[0];
        Assert.Equal("test-bucket", put.BucketName);
        Assert.Equal("k.bin", put.Key);
        Assert.Equal("application/octet-stream", put.ContentType);
        Assert.Empty(fake.InitiateRequests);
        Assert.Empty(fake.UploadPartRequests);
    }

    [Fact]
    public async Task LargePayload_UsesMultipart_AndAppliesPartCount()
    {
        var client = FakeAmazonS3.Create(out var fake);
        var stream = new S3UploadStream(client, "test-bucket", "big.bin", Options(), CancellationToken.None);
        var payload = new byte[80 * 1024 * 1024]; // 80 MiB exceeds 64 MiB multipart threshold
        await stream.WriteAsync(payload, 0, payload.Length);
        await stream.DisposeAsync();

        Assert.Empty(fake.PutObjectRequests);
        Assert.Single(fake.InitiateRequests);
        Assert.NotEmpty(fake.UploadPartRequests);
        Assert.Single(fake.CompleteRequests);
        Assert.Empty(fake.AbortRequests);
        Assert.True(fake.UploadPartRequests.Last().IsLastPart, "Final part must carry IsLastPart=true.");

        var totalBytes = fake.UploadPartRequests.Sum(p => p.PartSize);
        Assert.Equal(payload.Length, totalBytes);
    }

    [Fact]
    public async Task UploadPartFailure_TriggersAbort_AndPropagates()
    {
        var client = FakeAmazonS3.Create(out var fake);
        fake.ThrowOnNextUploadPart = new InvalidOperationException("boom");

        var stream = new S3UploadStream(client, "test-bucket", "big.bin", Options(), CancellationToken.None);
        var payload = new byte[80 * 1024 * 1024];
        await stream.WriteAsync(payload, 0, payload.Length);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(stream.DisposeAsync().AsTask);
        Assert.Equal("boom", ex.Message);
        Assert.Single(fake.AbortRequests);
        Assert.Equal("uploadid-test", fake.AbortRequests[0].UploadId);
    }

    [Fact]
    public async Task ApplyToPutObject_ForwardsSse_OnSinglePut()
    {
        var client = FakeAmazonS3.Create(out var fake);
        var stream = new S3UploadStream(client, "test-bucket", "k.bin", Options(sse: "AES256"), CancellationToken.None);
        await stream.WriteAsync(new byte[16], 0, 16);
        await stream.DisposeAsync();

        Assert.Single(fake.PutObjectRequests);
        Assert.Equal(Amazon.S3.ServerSideEncryptionMethod.AES256, fake.PutObjectRequests[0].ServerSideEncryptionMethod);
    }

    [Fact]
    public async Task ApplyToInitiateMultipart_ForwardsSse_OnMultipartInit()
    {
        var client = FakeAmazonS3.Create(out var fake);
        var stream = new S3UploadStream(client, "test-bucket", "big.bin", Options(sse: "aws:kms", key: "alias/test"), CancellationToken.None);
        var payload = new byte[80 * 1024 * 1024];
        await stream.WriteAsync(payload, 0, payload.Length);
        await stream.DisposeAsync();

        Assert.Single(fake.InitiateRequests);
        var init = fake.InitiateRequests[0];
        Assert.Equal(Amazon.S3.ServerSideEncryptionMethod.AWSKMS, init.ServerSideEncryptionMethod);
        Assert.Equal("alias/test", init.ServerSideEncryptionKeyManagementServiceKeyId);
    }

    [Fact]
    public void SyncDispose_WithPendingBytes_Throws_GuidingToAsyncDispose()
    {
        var client = FakeAmazonS3.Create(out _);
        var stream = new S3UploadStream(client, "test-bucket", "k.bin", Options(), CancellationToken.None);
        stream.Write(new byte[8], 0, 8);
        Assert.Throws<InvalidOperationException>(() => stream.Dispose());
    }
}
