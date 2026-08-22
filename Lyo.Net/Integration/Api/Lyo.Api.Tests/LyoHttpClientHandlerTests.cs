using System.Net;
using Lyo.Api.Client;

namespace Lyo.Api.Tests;

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
}
