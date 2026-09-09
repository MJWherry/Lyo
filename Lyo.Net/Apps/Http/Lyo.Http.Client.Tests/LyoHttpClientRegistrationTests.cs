using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Http.Client.Tests;

public sealed class LyoHttpClientRegistrationTests
{
    [Fact]
    public void AddLyoHttpClient_WithoutFactory_ResolvesTypedClient()
    {
        var services = new ServiceCollection();
        services.AddLyoHttpClient<LyoHttpClient, LyoHttpClientOptions>(o => o.BaseUrl = "https://example.test/");
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<LyoHttpClient>();
        Assert.NotNull(client);
        Assert.NotNull(client.Session);
        Assert.Equal(new Uri("https://example.test/"), client.GetClient().BaseAddress);
    }

    [Fact]
    public void AddLyoHttpClient_AfterHostConfigure_KeepsBoundBaseUrl()
    {
        var services = new ServiceCollection();
        services.Configure<LyoHttpClientOptions>(o => {
            o.BaseUrl = "http://localhost:5251/";
            o.RequestCompression = LyoHttpRequestCompressionType.Brotli;
        });
        services.AddLyoHttpClient<LyoHttpClient, LyoHttpClientOptions>();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LyoHttpClientOptions>>().Value;
        var client = provider.GetRequiredService<LyoHttpClient>();
        Assert.Equal("http://localhost:5251/", options.BaseUrl);
        Assert.Equal(LyoHttpRequestCompressionType.Brotli, options.RequestCompression);
        Assert.Equal(new Uri("http://localhost:5251/"), client.GetClient().BaseAddress);
    }
}
