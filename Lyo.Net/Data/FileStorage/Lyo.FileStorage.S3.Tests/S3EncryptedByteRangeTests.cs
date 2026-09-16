using Amazon.S3.Model;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.S3.Tests.Support;
using Lyo.IO.Temp.Models;
using Lyo.KeyStore;
using Lyo.Testing;
using HashAlgorithm = Lyo.FileMetadataStore.Models.HashAlgorithm;

namespace Lyo.FileStorage.S3.Tests;

public sealed class S3EncryptedByteRangeTests : IDisposable
{
    private readonly IOTempSession _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task OpenPlainByteRangeAsync_EncryptedObject_RequestsCiphertextOffset()
    {
        const string keyId = "enc-key";
        const int chunkSize = 32;
        using var encryption = CreateEncryption(keyId);
        var objects = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var options = new S3FileStorageOptions { BucketName = "bucket", HashAlgorithm = HashAlgorithm.Sha256 };
        using var service = new S3FileStorageService(
            options, new LocalFileMetadataStore(_temp.SessionDirectory), twoKeyEncryptionService: encryption, s3Client: FakeAmazonS3.Create(out var fake));
        fake.OnPutObject = req => {
            using var ms = new MemoryStream();
            req.InputStream.CopyTo(ms);
            objects[req.Key] = ms.ToArray();
            return new();
        };
        fake.OnGetObjectMetadata = req => objects.TryGetValue(req.Key, out var data)
            ? new() { ContentLength = data.Length }
            : throw new Amazon.S3.AmazonS3Exception("Not Found") { StatusCode = System.Net.HttpStatusCode.NotFound };
        fake.OnGetObject = req => {
            var data = objects[req.Key];
            if (req.ByteRange is null)
                return new() { ResponseStream = new MemoryStream(data) };

            var start = (int)req.ByteRange.Start;
            var end = (int)Math.Min(req.ByteRange.End, data.Length - 1);
            return new() { ResponseStream = new MemoryStream(data[start..(end + 1)]) };
        };

        var payload = TestData.Create(80, TestData.Seed + 9);
        var saved = await service.SaveFromStreamAsync(new MemoryStream(payload), payload.Length, "clip.mp4", encrypt: true, keyId: keyId, chunkSize: chunkSize, ct: TestCt);
        var stored = Assert.Single(objects).Value;
        using var layout = await encryption.ReadEncryptedStreamLayoutAsync(new MemoryStream(stored), TestCt);
        const int chunkIndex = 2;
        const long start = chunkIndex * 32 + 4;
        const long end = start + 7;
        fake.GetObjectRequests.Clear();

        await using var slice = await service.OpenPlainByteRangeAsync(saved, start, end, TestCt);
        using var buffered = new MemoryStream();
        await slice!.CopyToAsync(buffered, TestCt);

        Assert.Equal(payload[(int)start..((int)end + 1)], buffered.ToArray());
        var dataRead = fake.GetObjectRequests.Last();
        Assert.Equal(layout.HeaderLength + chunkIndex * layout.FrameSize, dataRead.ByteRange.Start);
    }

    private static TwoKeyEncryptionService<IEncryptionService, IEncryptionService> CreateEncryption(string keyId)
    {
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(keyId, "1", "test-kek-key-material-32bytes!!");
        keyStore.SetCurrentVersion(keyId, "1");
        return new(new AesGcmEncryptionService(keyStore), keyStore);
    }
}
