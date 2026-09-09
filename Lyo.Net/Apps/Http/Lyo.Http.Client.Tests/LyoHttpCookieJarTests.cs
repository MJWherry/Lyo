using Lyo.Http.Client;
using Lyo.Http.Client.Session;

namespace Lyo.Http.Client.Tests;

public sealed class LyoHttpCookieJarTests
{
    [Fact]
    public void Add_Remove_ToCookieHeader()
    {
        var jar = new LyoHttpCookieJar();
        var uri = new Uri("https://example.test/path");
        jar.Add(new() { Name = "a", Value = "1", Domain = "example.test", Path = "/" }, uri);
        jar.Add(new() { Name = "b", Value = "2", Domain = "example.test", Path = "/" }, uri);
        Assert.Contains("a=1", jar.ToCookieHeader(uri));
        jar.Remove("a");
        var header = jar.ToCookieHeader(uri);
        Assert.DoesNotContain("a=1", header);
        Assert.Contains("b=2", header);
        jar.Clear();
        Assert.Empty(jar.ToCookieHeader(uri));
    }
}
