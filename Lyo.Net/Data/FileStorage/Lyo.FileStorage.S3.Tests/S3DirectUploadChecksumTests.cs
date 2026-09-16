using System.Net;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Lyo.Common.Core.Identifiers;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.S3.Tests.Support;
using Lyo.Testing;
using HashAlgorithm = Lyo.FileMetadataStore.Models.HashAlgorithm;

namespace Lyo.FileStorage.S3.Tests;

public sealed class S3DirectUploadChecksumTests : IDisposable
{
    private const string ChecksumHeader = "x-amz-checksum-sha256";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "lyo-s3-direct-" + LyoGuid.CreateV7().ToString("N"));

    public S3DirectUploadChecksumTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private S3FileStorageService CreateService(out FakeAmazonS3 fake, Action<S3FileStorageOptions>? configure = null)
    {
        var options = new S3FileStorageOptions { BucketName = "bucket", HashAlgorithm = HashAlgorithm.Sha512 };
        configure?.Invoke(options);
        return new(options, new LocalFileMetadataStore(_root), s3Client: FakeAmazonS3.Create(out fake));
    }

    private static GetObjectAttributesResponse ReportSha256(byte[] payload)
        => new() {
            ObjectSize = payload.Length,
            Checksum = new() { ChecksumSHA256 = Convert.ToBase64String(SHA256.HashData(payload)), ChecksumType = ChecksumType.FULL_OBJECT }
        };

    [Fact]
    public async Task BeginDirectUploadAsync_ExpectedSha256_SignsChecksumHeaderIntoPresignedPut()
    {
        using var service = CreateService(out var fake);
        var sha = SHA256.HashData(TestData.Create(128, 1));
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = 128, OriginalFileName = "a.bin", ExpectedSha256 = sha }, TestCt);

        var presign = Assert.Single(fake.PreSignedUrlRequests);
        Assert.Equal(Convert.ToBase64String(sha), presign.Headers[ChecksumHeader]);
        Assert.NotNull(begin.RequiredPutHeaders);
        Assert.Equal(Convert.ToBase64String(sha), begin.RequiredPutHeaders[ChecksumHeader]);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_ObjectReportsSha256_FinalizesWithoutDownloading()
    {
        using var service = CreateService(out var fake);
        var payload = TestData.Create(4096, 2);
        var begin = await service.BeginDirectUploadAsync(
            new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin", ExpectedSha256 = SHA256.HashData(payload) }, TestCt);

        fake.OnGetObjectAttributes = _ => ReportSha256(payload);
        fake.OnGetObject = _ => throw new InvalidOperationException("Finalize must not download the object.");

        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);

        Assert.Equal(FileAvailability.Available, done.Availability);
        Assert.Equal(payload.Length, done.OriginalFileSize);
        Assert.Equal(SHA256.HashData(payload), done.OriginalFileHash);
        Assert.Equal(HashAlgorithm.Sha256, done.HashAlgorithm);
        var attrs = Assert.Single(fake.GetObjectAttributesRequests);
        Assert.Equal(begin.StorageLocation, attrs.Key);
        Assert.Contains(ObjectAttributes.Checksum, attrs.ObjectAttributes);
        Assert.Contains(ObjectAttributes.ObjectSize, attrs.ObjectAttributes);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_ReportedSha256DiffersFromDeclared_FailsAndStaysPending()
    {
        using var service = CreateService(out var fake);
        var payload = TestData.Create(4096, 3);
        var begin = await service.BeginDirectUploadAsync(
            new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin", ExpectedSha256 = SHA256.HashData(payload) }, TestCt);

        fake.OnGetObjectAttributes = _ => ReportSha256(TestData.Create(4096, 4));

        await Assert.ThrowsAsync<InvalidDataException>(() => service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt));

        Assert.Equal(FileAvailability.PendingDirectUpload, (await service.GetMetadataAsync(begin.FileId, TestCt)).Availability);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_HeaderInspection_RequestsOnlyAByteRangePrefix()
    {
        using var service = CreateService(out var fake, o => o.VerifyFileSignature = true);
        var payload = "%PDF-1.4\n"u8.ToArray().Concat(TestData.Create(64 * 1024, 5)).ToArray();
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "report.pdf" }, TestCt);
        fake.OnGetObjectAttributes = _ => ReportSha256(payload);
        fake.OnGetObject = req => {
            Assert.NotNull(req.ByteRange);
            var end = (int)Math.Min(req.ByteRange.End, payload.Length - 1);
            return new() { ResponseStream = new MemoryStream(payload[(int)req.ByteRange.Start..(end + 1)]) };
        };

        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);

        Assert.Equal(FileAvailability.Available, done.Availability);
        var get = Assert.Single(fake.GetObjectRequests);
        Assert.Equal(0, get.ByteRange.Start);
        Assert.InRange(get.ByteRange.End, 0, 1023);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_GetObjectAttributesNotImplemented_FallsBackToDownload()
    {
        using var service = CreateService(out var fake);
        var payload = TestData.Create(4096, 6);
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin" }, TestCt);
        fake.OnGetObject = _ => new() { ResponseStream = new MemoryStream(payload), ContentLength = payload.Length };

        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);

        Assert.Single(fake.GetObjectAttributesRequests);
        Assert.Equal(SHA512.HashData(payload), done.OriginalFileHash);
        Assert.Equal(HashAlgorithm.Sha512, done.HashAlgorithm);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_ObjectHasNoSha256_FallsBackToDownload()
    {
        using var service = CreateService(out var fake);
        var payload = TestData.Create(4096, 7);
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin" }, TestCt);
        fake.OnGetObjectAttributes = _ => new() { ObjectSize = payload.Length, Checksum = new() { ChecksumCRC64NVME = "AAAAAAAAAAA=" } };
        fake.OnGetObject = _ => new() { ResponseStream = new MemoryStream(payload), ContentLength = payload.Length };

        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);

        Assert.Single(fake.GetObjectAttributesRequests);
        Assert.Equal(SHA512.HashData(payload), done.OriginalFileHash);
    }

    [Fact]
    public async Task CompleteDirectUploadAsync_AttributesAccessDenied_FallsBackToDownload()
    {
        using var service = CreateService(out var fake);
        var payload = TestData.Create(1024, 8);
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin" }, TestCt);
        fake.OnGetObjectAttributes = _ => throw new AmazonS3Exception("Access Denied") { StatusCode = HttpStatusCode.Forbidden, ErrorCode = "AccessDenied" };
        fake.OnGetObject = _ => new() { ResponseStream = new MemoryStream(payload), ContentLength = payload.Length };

        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);

        Assert.Single(fake.GetObjectAttributesRequests);
        Assert.Equal(SHA512.HashData(payload), done.OriginalFileHash);
    }
}
