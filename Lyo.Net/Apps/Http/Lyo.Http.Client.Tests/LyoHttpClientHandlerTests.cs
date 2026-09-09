using System.Net;
using Lyo.Http.Client;

namespace Lyo.Http.Client.Tests;

public sealed class LyoHttpClientHandlerTests
{
    [Fact]
    public void Ctor_DefaultOptions_EnablesGzipDeflateBrotli()
    {
        using var handler = new LyoHttpClientHandler(new());
        Assert.Equal(DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli, handler.AutomaticDecompression);
    }

    [Fact]
    public void Ctor_Disabled_LeavesNone()
    {
        using var handler = new LyoHttpClientHandler(new() { EnableAutoResponseDecompression = false });
        Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression);
    }

    [Fact]
    public void Ctor_GzipOnly_SetsGzip()
    {
        using var handler = new LyoHttpClientHandler(new() { AcceptEncodings = ["gzip"] });
        Assert.Equal(DecompressionMethods.GZip, handler.AutomaticDecompression);
    }

    [Fact]
    public void ToDecompressionMethods_IgnoresUnknownEncodings()
    {
        var methods = LyoHttpClientHandler.ToDecompressionMethods(["gzip", "identity", "br"]);
        Assert.Equal(DecompressionMethods.GZip | DecompressionMethods.Brotli, methods);
    }

    [Fact]
    public void Ctor_ProxyUrl_SetsWebProxy()
    {
        using var handler = new LyoHttpClientHandler(new() { ProxyUrl = "http://127.0.0.1:8888", ProxyUsername = "u", ProxyPassword = "p" });
        Assert.True(handler.UseProxy);
        var proxy = Assert.IsType<WebProxy>(handler.Proxy);
        Assert.Equal(new Uri("http://127.0.0.1:8888/"), proxy.Address);
        var creds = Assert.IsType<NetworkCredential>(proxy.Credentials);
        Assert.Equal("u", creds.UserName);
        Assert.Equal("p", creds.Password);
    }
}
