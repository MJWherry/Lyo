using System.Security.Cryptography;
using Amazon.S3;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.S3.Tests.Support;
using Lyo.IO.Temp.Models;
using Lyo.Testing;
using HashAlgorithm = Lyo.FileMetadataStore.Models.HashAlgorithm;

namespace Lyo.FileStorage.S3.Tests;

public sealed class S3PlainByteRangeTests : IDisposable
{
    private readonly IOTempSession _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task OpenPlainByteRangeAsync_PlainObject_RequestsInclusiveByteRange()
    {
        var options = new S3FileStorageOptions { BucketName = "bucket", HashAlgorithm = HashAlgorithm.Sha256 };
        using var service = new S3FileStorageService(options, new LocalFileMetadataStore(_temp.SessionDirectory), s3Client: FakeAmazonS3.Create(out var fake));
        var payload = TestData.Create(64, TestData.Seed + 1);
        var begin = await service.BeginDirectUploadAsync(
            new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "clip.mp4", ExpectedSha256 = SHA256.HashData(payload) }, TestCt);

        fake.OnGetObjectAttributes = _ => new() {
            ObjectSize = payload.Length, Checksum = new() { ChecksumSHA256 = Convert.ToBase64String(SHA256.HashData(payload)), ChecksumType = ChecksumType.FULL_OBJECT }
        };

        await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);
        fake.GetObjectRequests.Clear();
        fake.OnGetObject = req => {
            if (req.ByteRange is null)
                return new() { ResponseStream = new MemoryStream(payload) };

            var start = (int)req.ByteRange.Start;
            var end = (int)Math.Min(req.ByteRange.End, payload.Length - 1);
            return new() { ResponseStream = new MemoryStream(payload[start..(end + 1)]) };
        };

        await using var slice = await service.OpenPlainByteRangeAsync(begin.FileId, null, 2, 5, TestCt);
        using var buffered = new MemoryStream();
        await slice!.CopyToAsync(buffered, TestCt);
        Assert.Equal(payload[2..6], buffered.ToArray());
        var get = Assert.Single(fake.GetObjectRequests);
        Assert.Equal(2, get.ByteRange.Start);
        Assert.Equal(5, get.ByteRange.End);
    }
}