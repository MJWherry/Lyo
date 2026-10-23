using System.Net;
using System.Text;
using Lyo.Http.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.ClamAv.Client.Tests;

public sealed class ClamAvClientTests
{
    [Fact]
    public async Task ScanAsync_OkStatus_ReturnsClean()
    {
        var handler = new RecordingHandler { Respond = _ => Json(HttpStatusCode.OK, """{"status":"OK"}""") };
        var client = Client(handler);
        var result = await client.ScanAsync(Payload(), "sample.bin", TestCt);
        Assert.Equal(ClamScanStatus.Clean, result.Status);
        Assert.Null(result.Signature);
    }

    [Fact]
    public async Task ScanAsync_FoundStatus_ReturnsSignature()
    {
        var handler = new RecordingHandler {
            Respond = _ => Json(HttpStatusCode.NotAcceptable, """{"Status":"FOUND","Description":"Win.Test.EICAR_HDB-1"}""")
        };
        var client = Client(handler);
        var result = await client.ScanAsync(Payload(), "sample.bin", TestCt);
        Assert.Equal(ClamScanStatus.Infected, result.Status);
        Assert.Equal("Win.Test.EICAR_HDB-1", result.Signature);
    }

    [Fact]
    public async Task ScanAsync_OtherStatus_ReturnsError()
    {
        var handler = new RecordingHandler { Respond = _ => Json(HttpStatusCode.OK, """{"status":"ERROR"}""") };
        var client = Client(handler);
        var result = await client.ScanAsync(Payload(), "sample.bin", TestCt);
        Assert.Equal(ClamScanStatus.Error, result.Status);
        Assert.Null(result.Signature);
    }

    [Fact]
    public async Task ScanAsync_PostsMultipartFileToScan()
    {
        var handler = new RecordingHandler { Respond = _ => Json(HttpStatusCode.OK, """{"status":"OK"}""") };
        var client = Client(handler);
        await client.ScanAsync(Payload(), "sample.bin", TestCt);
        var call = handler.Calls[0];
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.EndsWith("/scan", call.Uri.AbsolutePath);
        var disposition = call.Body!.Replace("\"", "");
        Assert.Contains("name=file", disposition, StringComparison.Ordinal);
        Assert.Contains("sample.bin", call.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScanAsync_ServerError_Throws()
    {
        var handler = new RecordingHandler { Respond = _ => new(HttpStatusCode.InternalServerError) };
        var client = Client(handler);
        await Assert.ThrowsAsync<LyoHttpException>(() => client.ScanAsync(Payload(), "sample.bin", TestCt));
    }

    [Fact]
    public async Task GetVersionAsync_ReturnsPlainText()
    {
        var handler = new RecordingHandler { Respond = _ => new(HttpStatusCode.OK) { Content = new StringContent("ClamAV 1.4.3/27600\n", Encoding.UTF8) } };
        var client = Client(handler);
        var version = await client.GetVersionAsync(TestCt);
        Assert.Equal("ClamAV 1.4.3/27600", version);
        Assert.Equal(HttpMethod.Get, handler.Calls[0].Method);
        Assert.EndsWith("/version", handler.Calls[0].Uri.AbsolutePath);
    }

    [Fact]
    public void AddClamAvClient_MissingBaseUrl_Throws()
    {
        var services = new ServiceCollection();
        Assert.ThrowsAny<ArgumentException>(() => services.AddClamAvClient(new ClamAvClientOptions()));
    }

    private static ClamAvClient Client(HttpMessageHandler handler)
        => new(new ClamAvClientOptions { BaseUrl = "http://clam.example/" }, httpClient: new HttpClient(handler));

    private static MemoryStream Payload() => new([1, 2, 3, 4]);

    private static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
