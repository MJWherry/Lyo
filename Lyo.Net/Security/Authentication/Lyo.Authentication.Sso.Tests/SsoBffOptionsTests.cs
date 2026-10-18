using Lyo.Authentication.Sso.Completion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.Sso.Tests;

public sealed class SsoBffOptionsTests
{
    [Fact]
    public void Bind_LyoSsoAbsent_ReadsLyoOidcBff()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["LyoOidcBff:AllowedReturnOrigins:0"] = "https://app.example",
            ["LyoOidcBff:DefaultReturnUrl"] = "/app",
            ["LyoOidcBff:HandoffCodeTtl"] = "00:00:45",
            ["LyoExternalLogin:Policy"] = "RequireExistingUser"
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLyoSso(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<SsoBffOptions>();
        Assert.Equal(["https://app.example"], options.AllowedReturnOrigins);
        Assert.Equal("/app", options.DefaultReturnUrl);
        Assert.Equal(TimeSpan.FromSeconds(45), options.HandoffCodeTtl);
        Assert.Equal(ExternalLoginPolicy.RequireExistingUser, provider.GetRequiredService<ExternalLoginOptions>().Policy);
    }

    [Fact]
    public void Bind_LyoSsoPresentAndEmpty_DoesNotReadLyoOidcBff()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["LyoSso:DefaultReturnUrl"] = "/",
            ["LyoOidcBff:AllowedReturnOrigins:0"] = "https://old.example",
            ["LyoOidcBff:DefaultReturnUrl"] = "/old",
            ["LyoOidcBff:HandoffCodeTtl"] = "00:02:00"
        }).Build();

        var options = new SsoBffOptions();
        Extensions.BindBff(configuration, options);
        Assert.Empty(options.AllowedReturnOrigins);
        Assert.Equal("/", options.DefaultReturnUrl);
        Assert.Equal(TimeSpan.FromSeconds(30), options.HandoffCodeTtl);
    }
}
