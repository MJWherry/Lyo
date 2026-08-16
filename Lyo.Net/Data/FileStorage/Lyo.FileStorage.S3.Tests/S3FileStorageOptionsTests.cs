namespace Lyo.FileStorage.S3.Tests;

/// <summary>Pure-data coverage for the <see cref="S3FileStorageOptions" /> surface (binding section, base defaults).</summary>
public sealed class S3FileStorageOptionsTests
{
    [Fact]
    public void SectionName_IsStable()
        =>
            // Public binding contract for appsettings.json.
            Assert.Equal("S3FileStorageOptions", S3FileStorageOptions.SectionName);

    [Fact]
    public void EnableMetrics_DefaultsToBaseTrue()
    {
        // p5-options-defaults invariant: base defaults stay true for S3 backend.
        var opts = new S3FileStorageOptions { BucketName = "bucket" };
        Assert.True(opts.EnableMetrics);
    }

    [Fact]
    public void NullSseFields_AreNotApplied()
    {
        // Confirms BuildRequiredPutHeaders returns null when no SSE configured (parity with default flow).
        var opts = new S3FileStorageOptions { BucketName = "bucket" };
        Assert.Null(opts.ServerSideEncryption);
        Assert.Null(opts.ServerSideEncryptionAwsKmsKeyId);
    }

    [Fact]
    public void Profile_DefaultsToNull()
        => Assert.Null(new S3FileStorageOptions { BucketName = "bucket" }.Profile);
}