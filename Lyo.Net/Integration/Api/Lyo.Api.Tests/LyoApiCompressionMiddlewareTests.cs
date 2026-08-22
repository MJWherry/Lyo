using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using Lyo.Api;
using Lyo.Common.Records;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Lyo.Api.Tests;

public sealed class LyoApiCompressionMiddlewareTests
{
    private static readonly string Payload = string.Join('\n', Enumerable.Repeat("name,value,extra", 80));

    [Fact]
    public async Task Csv_WithBrotliAcceptEncoding_IsCompressed()
    {
        await using var app = await StartAppAsync();
        using var response = await SendAsync(app, "/csv");
        AssertCompressed(response);
        var body = DecompressBrotli(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal(Payload, Encoding.UTF8.GetString(body));
    }

    [Fact]
    public async Task OctetStream_WithBrotliAcceptEncoding_IsCompressed()
    {
        await using var app = await StartAppAsync();
        using var response = await SendAsync(app, "/bin");
        AssertCompressed(response);
    }

    [Fact]
    public async Task Svg_WithBrotliAcceptEncoding_IsCompressed()
    {
        await using var app = await StartAppAsync();
        using var response = await SendAsync(app, "/svg");
        AssertCompressed(response);
    }

    [Fact]
    public async Task Jpeg_WithBrotliAcceptEncoding_IsNotCompressed()
    {
        await using var app = await StartAppAsync();
        using var response = await SendAsync(app, "/jpeg");
        Assert.DoesNotContain("br", response.Content.Headers.ContentEncoding, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Zip_WithBrotliAcceptEncoding_IsNotCompressed()
    {
        await using var app = await StartAppAsync();
        using var response = await SendAsync(app, "/zip");
        Assert.DoesNotContain("br", response.Content.Headers.ContentEncoding, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<WebApplication> StartAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLyoApiCompression();
        var app = builder.Build();
        app.UseLyoApiCompression();
        var bytes = Encoding.UTF8.GetBytes(Payload);
        app.MapGet("/csv", () => Results.Text(Payload, FileTypeInfo.Csv.MimeType));
        app.MapGet("/bin", () => Results.Bytes(bytes, FileTypeInfo.Unknown.MimeType));
        app.MapGet("/svg", () => Results.Text("<svg xmlns='http://www.w3.org/2000/svg'><text>ok</text></svg>", FileTypeInfo.Svg.MimeType));
        app.MapGet("/jpeg", () => Results.Bytes(bytes, FileTypeInfo.Jpeg.MimeType));
        app.MapGet("/zip", () => Results.Bytes(bytes, FileTypeInfo.Zip.MimeType));
        await app.StartAsync();
        return app;
    }

    private static async Task<HttpResponseMessage> SendAsync(WebApplication app, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        var response = await app.GetTestClient().SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private static void AssertCompressed(HttpResponseMessage response)
        => Assert.Contains("br", response.Content.Headers.ContentEncoding, StringComparer.OrdinalIgnoreCase);

    private static byte[] DecompressBrotli(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }
}
