using FileStorageApi = Lyo.Api.FileStorage.Extensions;
using Lyo.Api.FileStorage;
using Lyo.Compression;
using Lyo.Compression.Compressors;
using Lyo.Compression.Models;
using Lyo.Encryption;
using Lyo.Encryption.AesGcm;
using Lyo.Encryption.TwoKey;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage;
using Lyo.FileStorage.Models;
using Lyo.IO.Temp.Models;
using Lyo.KeyStore;
using Lyo.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.FileStorage.Tests;

public sealed class StoredDownloadRangeTests : IDisposable
{
    private readonly IOTempSession _temp = new();

    public void Dispose() => _temp.Dispose();

    [Theory]
    [InlineData("bytes=0-99", 1000, "Partial", 0L, 99L)]
    [InlineData("bytes=100-", 1000, "Partial", 100L, 999L)]
    [InlineData("bytes=-50", 1000, "Partial", 950L, 999L)]
    [InlineData("bytes=-5000", 1000, "Partial", 0L, 999L)]
    [InlineData("bytes=990-5000", 1000, "Partial", 990L, 999L)]
    [InlineData("bytes=1000-1005", 1000, "Unsatisfiable", 0L, 0L)]
    [InlineData("bytes=0-1,2-3", 1000, "Full", 0L, 0L)]
    [InlineData("not-a-range", 1000, "Full", 0L, 0L)]
    [InlineData(null, 1000, "Full", 0L, 0L)]
    public void Decide_Seekable_ResolvesSingleRange(string? header, long total, string kind, long start, long end)
    {
        var decision = StoredByteRange.Decide(canServe: true, total, header);
        var expected = Enum.Parse<StoredDownloadKind>(kind);

        Assert.Equal(expected, decision.Kind);
        Assert.True(decision.AcceptRanges);
        if (expected == StoredDownloadKind.Partial) {
            Assert.Equal(start, decision.Start);
            Assert.Equal(end, decision.EndInclusive);
        }
    }

    [Fact]
    public void Decide_NotSeekable_IgnoresHeader()
    {
        var decision = StoredByteRange.Decide(canServe: false, total: 1000, rangeHeader: "bytes=0-1");

        Assert.Equal(StoredDownloadKind.Full, decision.Kind);
        Assert.False(decision.AcceptRanges);
    }

    [Theory]
    [InlineData("video/mp4", false, true, true)]
    [InlineData("audio/mpeg", false, true, true)]
    [InlineData("text/plain", false, true, true)]
    [InlineData("application/pdf", false, true, true)]
    [InlineData("application/octet-stream", false, true, true)]
    [InlineData("video/mp4", true, true, false)]
    [InlineData("text/plain", true, true, false)]
    [InlineData("video/mp4", false, false, false)]
    public void CanServe_RequiresUncompressedAndARangedBackend(string contentType, bool compressed, bool backend, bool expected)
    {
        _ = contentType;
        Assert.Equal(expected, StoredByteRange.CanServe(compressed, backend));
    }

    [Fact]
    public async Task Download_PlainRange_ReturnsSlice()
    {
        var payload = TestData.Create(32, TestData.Seed + 2);
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(payload, "clip.mp4", ct: TestCt);

        var http = await DownloadAsync(storage, saved, "bytes=2-5");

        Assert.Equal(StatusCodes.Status206PartialContent, http.Response.StatusCode);
        Assert.Equal("bytes", http.Response.Headers.AcceptRanges.ToString());
        Assert.Equal($"bytes 2-5/{payload.Length}", http.Response.Headers.ContentRange.ToString());
        Assert.Equal(4, http.Response.ContentLength);
        Assert.False(http.Response.Headers.ContainsKey("Content-Digest"));
        Assert.Equal(payload[2..6], Body(http));
    }

    [Fact]
    public async Task Download_PlainOpenRange_BodyStartsAtOffset()
    {
        var payload = TestData.Create(32, TestData.Seed + 6);
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(payload, "clip.mp4", ct: TestCt);

        var http = await DownloadAsync(storage, saved, "bytes=8-");

        Assert.Equal(StatusCodes.Status206PartialContent, http.Response.StatusCode);
        Assert.Equal($"bytes 8-{payload.Length - 1}/{payload.Length}", http.Response.Headers.ContentRange.ToString());
        Assert.Equal(payload[8..], Body(http));
    }

    [Fact]
    public async Task Download_PlainRangePastEnd_Returns416()
    {
        var payload = TestData.Create(32, TestData.Seed + 3);
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(payload, "clip.mp4", ct: TestCt);

        var http = await DownloadAsync(storage, saved, "bytes=32-40");

        Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, http.Response.StatusCode);
        Assert.Equal("bytes", http.Response.Headers.AcceptRanges.ToString());
        Assert.Equal($"bytes */{payload.Length}", http.Response.Headers.ContentRange.ToString());
        Assert.Empty(Body(http));
    }

    [Fact]
    public async Task Download_PlainWithoutRange_AdvertisesAcceptRanges()
    {
        var payload = TestData.Create(32, TestData.Seed + 4);
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(payload, "clip.mp4", ct: TestCt);

        var http = await DownloadAsync(storage, saved, range: null);

        Assert.Equal(StatusCodes.Status200OK, http.Response.StatusCode);
        Assert.Equal("bytes", http.Response.Headers.AcceptRanges.ToString());
        Assert.Equal(payload, Body(http));
    }

    [Fact]
    public async Task Download_PlainTextRange_ReturnsSlice()
    {
        var payload = TestData.Create(32, TestData.Seed + 5);
        using var storage = CreateStorage();
        var saved = await storage.SaveFileAsync(payload, "notes.txt", ct: TestCt);

        var http = await DownloadAsync(storage, saved, "bytes=2-5");

        Assert.Equal(StatusCodes.Status206PartialContent, http.Response.StatusCode);
        Assert.Equal("bytes", http.Response.Headers.AcceptRanges.ToString());
        Assert.Equal($"bytes 2-5/{payload.Length}", http.Response.Headers.ContentRange.ToString());
        Assert.Equal(payload[2..6], Body(http));
    }

    [Fact]
    public async Task Download_EncryptedRange_ReturnsPlaintextSlice()
    {
        const string keyId = "enc-key";
        const int chunkSize = 32;
        var payload = TestData.Create(80, TestData.Seed + 10);
        using var encryption = CreateEncryption(keyId);
        using var storage = CreateStorage(encryption);
        var saved = await storage.SaveFileAsync(payload, "clip.mp4", encrypt: true, keyId: keyId, chunkSize: chunkSize, ct: TestCt);
        const int start = 2 * chunkSize + 4;

        var http = await DownloadAsync(storage, saved, $"bytes={start}-{start + 7}");

        Assert.Equal(StatusCodes.Status206PartialContent, http.Response.StatusCode);
        Assert.Equal($"bytes {start}-{start + 7}/{payload.Length}", http.Response.Headers.ContentRange.ToString());
        Assert.Equal(payload[start..(start + 8)], Body(http));
    }

    [Fact]
    public async Task Download_EncryptedCompressed_IgnoresRange()
    {
        const string keyId = "enc-key";
        var payload = TestData.Create(80, TestData.Seed + 11);
        using var encryption = CreateEncryption(keyId);
        var compression = new CompressionService([new GZipCompressorFactory()], options: new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.GZip });
        using var storage = CreateStorage(encryption, compression);
        var saved = await storage.SaveFileAsync(payload, "clip.mp4", compress: true, encrypt: true, keyId: keyId, ct: TestCt);

        var http = await DownloadAsync(storage, saved, "bytes=8-15");

        Assert.Equal(StatusCodes.Status200OK, http.Response.StatusCode);
        Assert.True(string.IsNullOrEmpty(http.Response.Headers.AcceptRanges.ToString()));
        Assert.Equal(payload, Body(http));
    }

    private LocalFileStorageService CreateStorage(ITwoKeyEncryptionService? encryption = null, ICompressionService? compression = null)
        => new(new() { RootDirectoryPath = _temp.SessionDirectory }, compressionService: compression, twoKeyEncryptionService: encryption);

    private static TwoKeyEncryptionService<IEncryptionService, IEncryptionService> CreateEncryption(string keyId)
    {
        var keyStore = new LocalKeyStore();
        keyStore.AddKeyFromString(keyId, "1", "test-kek-key-material-32bytes!!");
        keyStore.SetCurrentVersion(keyId, "1");
        return new(new AesGcmEncryptionService(keyStore), keyStore);
    }

    private static async Task<HttpContext> DownloadAsync(LocalFileStorageService storage, FileStoreResult saved, string? range)
    {
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        http.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        if (range != null)
            http.Request.Headers.Range = range;

        var result = await FileStorageApi.StreamStoredFileAsync(http, storage, saved.Id, saved, inline: false, TestCt);
        await result.ExecuteAsync(http);
        return http;
    }

    private static byte[] Body(HttpContext http)
    {
        var body = (MemoryStream)http.Response.Body;
        body.Position = 0;
        return body.ToArray();
    }
}
