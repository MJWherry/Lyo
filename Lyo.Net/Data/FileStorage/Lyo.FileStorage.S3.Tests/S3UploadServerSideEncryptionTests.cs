using Amazon.S3;
using Amazon.S3.Model;

namespace Lyo.FileStorage.S3.Tests;

/// <summary>Pure-logic coverage for the SSE/header builder used by S3 PUT signing and SDK PUT/Copy/Multipart requests.</summary>
public sealed class S3UploadServerSideEncryptionTests
{
    [Fact]
    public void BuildRequiredPutHeaders_AES256_OnlyAddsEncryptionHeader()
    {
        var headers = S3UploadServerSideEncryption.BuildRequiredPutHeaders(new() { BucketName = "b", ServerSideEncryption = "AES256" }, null);
        Assert.NotNull(headers);
        Assert.Equal("AES256", headers["x-amz-server-side-encryption"]);
        Assert.False(headers.ContainsKey("x-amz-server-side-encryption-aws-kms-key-id"));
    }

    [Fact]
    public void BuildRequiredPutHeaders_KmsWithKey_IncludesKeyId()
    {
        var headers = S3UploadServerSideEncryption.BuildRequiredPutHeaders(
            new() { BucketName = "b", ServerSideEncryption = "aws:kms", ServerSideEncryptionAwsKmsKeyId = "alias/test" }, null);

        Assert.NotNull(headers);
        Assert.Equal("aws:kms", headers["x-amz-server-side-encryption"]);
        Assert.Equal("alias/test", headers["x-amz-server-side-encryption-aws-kms-key-id"]);
    }

    [Fact]
    public void BuildRequiredPutHeaders_DsseWithKey_IncludesKeyId()
    {
        var headers = S3UploadServerSideEncryption.BuildRequiredPutHeaders(
            new() { BucketName = "b", ServerSideEncryption = "aws:kms:dsse", ServerSideEncryptionAwsKmsKeyId = "arn:aws:kms:..." }, null);

        Assert.NotNull(headers);
        Assert.Equal("aws:kms:dsse", headers["x-amz-server-side-encryption"]);
        Assert.Equal("arn:aws:kms:...", headers["x-amz-server-side-encryption-aws-kms-key-id"]);
    }

    [Fact]
    public void BuildRequiredPutHeaders_ContentType_PropagatesValue()
    {
        var headers = S3UploadServerSideEncryption.BuildRequiredPutHeaders(new() { BucketName = "b" }, "application/pdf");
        Assert.NotNull(headers);
        Assert.Equal("application/pdf", headers["Content-Type"]);
    }

    [Fact]
    public void BuildRequiredPutHeaders_NoOptions_ReturnsNull()
    {
        var headers = S3UploadServerSideEncryption.BuildRequiredPutHeaders(new() { BucketName = "b" }, null);
        Assert.Null(headers);
    }

    [Fact]
    public void ApplyToPutObject_AES256_SetsMethod()
    {
        var req = new PutObjectRequest { BucketName = "bbb", Key = "k" };
        S3UploadServerSideEncryption.ApplyToPutObject(req, new() { BucketName = "bbb", ServerSideEncryption = "AES256" });
        Assert.Equal(ServerSideEncryptionMethod.AES256, req.ServerSideEncryptionMethod);
    }

    [Fact]
    public void ApplyToPutObject_Kms_SetsMethodAndKey()
    {
        var req = new PutObjectRequest { BucketName = "bbb", Key = "k" };
        S3UploadServerSideEncryption.ApplyToPutObject(req, new() { BucketName = "bbb", ServerSideEncryption = "aws:kms", ServerSideEncryptionAwsKmsKeyId = "alias/test" });
        Assert.Equal(ServerSideEncryptionMethod.AWSKMS, req.ServerSideEncryptionMethod);
        Assert.Equal("alias/test", req.ServerSideEncryptionKeyManagementServiceKeyId);
    }

    [Fact]
    public void ApplyToPutObject_Invalid_Throws()
    {
        var req = new PutObjectRequest { BucketName = "bbb", Key = "k" };
        Assert.Throws<InvalidOperationException>(() => S3UploadServerSideEncryption.ApplyToPutObject(req, new() { BucketName = "bbb", ServerSideEncryption = "garbage" }));
    }
}