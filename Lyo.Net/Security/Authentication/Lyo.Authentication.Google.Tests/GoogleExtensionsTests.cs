using Lyo.Authentication.OpenIdConnect.Provider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.Google.Tests;

public sealed class GoogleExtensionsTests
{
    [Fact]
    public void AddGoogleProvider_RegistersProviderAndOptions()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddGoogleProvider(o => {
            o.ClientId = "client";
            o.ClientSecret = "secret";
            o.RedirectUri = "https://x/callback";
        });

        var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IOpenIdConnectProvider>();
        Assert.IsType<GoogleOpenIdConnectProvider>(provider);
        Assert.Equal(GoogleOptions.DefaultName, provider.Name);
    }

    [Fact]
    public void AddGoogleProviderFromConfiguration_BindsSection()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    ["GoogleAuth:ClientId"] = "client-from-config",
                    ["GoogleAuth:ClientSecret"] = "secret-from-config",
                    ["GoogleAuth:RedirectUri"] = "https://api/callback",
                    ["GoogleAuth:HostedDomain"] = "lyolabs.io"
                })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddGoogleProviderFromConfiguration(configuration);
        var sp = services.BuildServiceProvider();
        var provider = (GoogleOpenIdConnectProvider)sp.GetRequiredService<IOpenIdConnectProvider>();
        Assert.Equal("client-from-config", provider.ClientId);
        Assert.Equal("HostedDomainMismatch", provider.PreflightReject(new Dictionary<string, object?>()));
    }
}