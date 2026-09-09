using System.Net;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions.Models;
using Lyo.Http.Client;
using Lyo.Http.Client.Pipeline;
using Lyo.Http.Client.Plan;

namespace Lyo.Http.Client.Tests;

public sealed class LyoHttpDownloadTests
{
    [Fact]
    public async Task GetFileStreamAsync_StreamsBytesAndReportsName()
    {
        var payload = "hello-file"u8.ToArray();
        var inner = new StubBytesHandler(payload, "gallery.bin");
        using var client = new LyoHttpClient(new HttpClient(inner) { BaseAddress = new("https://example.test/") });
        var (stream, fileName, length) = await client.GetFileStreamAsync("/dl/gallery.bin", ct: TestContext.Current.CancellationToken);
        await using (stream) {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, TestContext.Current.CancellationToken);
            Assert.Equal(payload, ms.ToArray());
        }

        Assert.Equal("gallery.bin", fileName);
        Assert.Equal(payload.Length, length);
    }

    [Fact]
    public async Task GetFileAsync_MarksStreamBinary()
    {
        var inner = new MarkerHandler();
        using var client = new LyoHttpClient(new HttpClient(inner) { BaseAddress = new("https://example.test/") });
        await client.GetFileAsync("/dl/a.bin", ct: TestContext.Current.CancellationToken);
        Assert.True(inner.StreamBinary);
    }

    [Fact]
    public async Task GetFileWithTypeAsync_MarksStreamBinary()
    {
        var inner = new MarkerHandler();
        using var client = new LyoHttpClient(new HttpClient(inner) { BaseAddress = new("https://example.test/") });
        await client.GetFileWithTypeAsync("/dl/a.bin", ct: TestContext.Current.CancellationToken);
        Assert.True(inner.StreamBinary);
    }

    [Fact]
    public async Task DownloadToFileAsync_WritesAndReportsProgress()
    {
        var payload = new byte[4096];
        Random.Shared.NextBytes(payload);
        var inner = new StubBytesHandler(payload, "out.bin");
        using var client = new LyoHttpClient(new(inner) { BaseAddress = new("https://example.test/") });
        var reports = new List<LyoHttpDownloadProgress>();
        var dest = Path.Combine(Path.GetTempPath(), "lyo-http-dl-" + Guid.NewGuid().ToString("N"), "out.bin");
        try {
            var saved = await client.DownloadToFileAsync("/dl/out.bin", dest, new Progress<LyoHttpDownloadProgress>(reports.Add), ct: TestContext.Current.CancellationToken);
            Assert.Equal(dest, saved);
            Assert.Equal(payload, await File.ReadAllBytesAsync(dest, TestContext.Current.CancellationToken));
            Assert.NotEmpty(reports);
            Assert.Equal(payload.Length, reports[^1].BytesReceived);
        }
        finally {
            if (File.Exists(dest))
                File.Delete(dest);
            var dir = Path.GetDirectoryName(dest);
            if (dir != null && Directory.Exists(dir))
                Directory.Delete(dir);
        }
    }

    private sealed class StubBytesHandler(byte[] payload, string fileName) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new ByteArrayContent(payload)
            };
            response.Content.Headers.ContentLength = payload.Length;
            response.Content.Headers.ContentDisposition = new("attachment") { FileName = fileName };
            return Task.FromResult(response);
        }
    }

    private sealed class MarkerHandler : HttpMessageHandler
    {
        public bool StreamBinary { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            StreamBinary = LyoHttpRequestMarkers.IsStreamBinary(request);
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            return Task.FromResult(response);
        }
    }
}

