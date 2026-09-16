using System.Net;
using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Lyo.Common.Core.Identifiers;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Models;
using Lyo.Testing;
using HashAlgorithm = Lyo.FileMetadataStore.Models.HashAlgorithm;

namespace Lyo.FileStorage.AzureBlob.Tests;

/// <summary>Presigned direct upload against Azurite: a real SAS PUT, then finalize from the blob's stored Content-MD5.</summary>
[Trait("Category", "Integration")]
public sealed class AzureBlobDirectUploadChecksumTests(AzuriteFixture fixture) : IClassFixture<AzuriteFixture>, IDisposable
{
    private readonly string _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "lyo-blob-direct-" + LyoGuid.CreateV7().ToString("N"))).FullName;

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private async Task<AzureBlobFileStorageService> CreateServiceAsync(Action<AzureBlobFileStorageOptions>? configure = null)
    {
        Assert.SkipWhen(fixture.ConnectionString is null, "Docker is unavailable, so Azurite cannot start.");
        var options = new AzureBlobFileStorageOptions {
            ConnectionString = fixture.ConnectionString!,
            ContainerName = $"c-{LyoGuid.CreateV7():N}",
            HashAlgorithm = HashAlgorithm.Sha512
        };

        configure?.Invoke(options);
        await new BlobContainerClient(options.ConnectionString, options.ContainerName).CreateIfNotExistsAsync(cancellationToken: TestCt);
        return new(options, new LocalFileMetadataStore(_root));
    }

    private static async Task<HttpStatusCode> PutAsync(DirectUploadBeginResult begin, byte[] body)
    {
        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, begin.PresignedPutUrl) { Content = new ByteArrayContent(body) };
        foreach (var (name, value) in begin.RequiredPutHeaders ?? new Dictionary<string, string>()) {
            if (!request.Headers.TryAddWithoutValidation(name, value))
                request.Content.Headers.TryAddWithoutValidation(name, value);
        }

        using var response = await http.SendAsync(request, TestCt);
        return response.StatusCode;
    }

    [Fact]
    public async Task DirectUpload_ContentMd5Declared_RequiresHeaderAndFinalizesFromBlobProperties()
    {
        using var service = await CreateServiceAsync();
        var payload = TestData.Create(8192, 1);
        var md5 = MD5.HashData(payload);
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin", ExpectedContentMd5 = md5 }, TestCt);

        Assert.NotNull(begin.RequiredPutHeaders);
        Assert.Equal(Convert.ToBase64String(md5), begin.RequiredPutHeaders["Content-MD5"]);
        Assert.Equal(HttpStatusCode.Created, await PutAsync(begin, payload));

        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);

        Assert.Equal(FileAvailability.Available, done.Availability);
        Assert.Equal(payload.Length, done.OriginalFileSize);
        Assert.Equal(md5, done.OriginalFileHash);
        Assert.Equal(HashAlgorithm.Md5, done.HashAlgorithm);
    }

    [Fact]
    public async Task DirectUpload_BodyDiffersFromDeclaredMd5_BlobRejectsThePut()
    {
        using var service = await CreateServiceAsync();
        var payload = TestData.Create(4096, 2);
        var begin = await service.BeginDirectUploadAsync(
            new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "a.bin", ExpectedContentMd5 = MD5.HashData(payload) }, TestCt);

        Assert.Equal(HttpStatusCode.BadRequest, await PutAsync(begin, TestData.Create(4096, 3)));
    }

    [Fact]
    public async Task DirectUpload_NoChecksumDeclared_FinalizesFromServiceComputedMd5AndHeaderPrefix()
    {
        using var service = await CreateServiceAsync(o => o.VerifyFileSignature = true);
        var payload = "%PDF-1.4\n"u8.ToArray().Concat(TestData.Create(64 * 1024, 4)).ToArray();
        var begin = await service.BeginDirectUploadAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "report.pdf" }, TestCt);
        Assert.Equal(HttpStatusCode.Created, await PutAsync(begin, payload));

        var done = await service.CompleteDirectUploadAsync(begin.FileId, ct: TestCt);

        Assert.Equal(FileAvailability.Available, done.Availability);
        Assert.Equal(MD5.HashData(payload), done.OriginalFileHash);
        Assert.Equal(HashAlgorithm.Md5, done.HashAlgorithm);
    }
}
