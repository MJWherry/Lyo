using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Google.Tests;

public sealed class GoogleOpenIdConnectProviderTests
{
    [Fact]
    public void Provider_ExposesDiscoveryUrlAndDefaultScopes()
    {
        var provider = NewProvider();
        Assert.Equal(GoogleOptions.DefaultName, provider.Name);
        Assert.Equal(GoogleOptions.DiscoveryUrl, provider.DiscoveryUrl);
        Assert.Equal(["openid", "email", "profile"], provider.Scopes);
    }

    [Fact]
    public void Provider_RejectsMissingClientId()
    {
        Assert.Throws<System.ArgumentException>(() => new GoogleOpenIdConnectProvider(MsOptions.Create(new GoogleOptions {
            ClientId = string.Empty,
            ClientSecret = "secret",
            RedirectUri = "https://x"
        })));
    }

    [Fact]
    public void PreflightReject_AllowsWhenHostedDomainUnset()
    {
        var provider = NewProvider();
        Assert.Null(provider.PreflightReject(new Dictionary<string, object?>()));
    }

    [Fact]
    public void PreflightReject_RejectsPersonalAccountWhenHostedDomainSet()
    {
        var provider = NewProvider(hd: "lyolabs.io");
        var reason = provider.PreflightReject(new Dictionary<string, object?>());
        Assert.Equal("HostedDomainMismatch", reason);
    }

    [Fact]
    public void PreflightReject_RejectsWrongHostedDomain()
    {
        var provider = NewProvider(hd: "lyolabs.io");
        var reason = provider.PreflightReject(new Dictionary<string, object?> { ["hd"] = "nope.io" });
        Assert.Equal("HostedDomainMismatch", reason);
    }

    [Fact]
    public void PreflightReject_AcceptsCorrectHostedDomain()
    {
        var provider = NewProvider(hd: "lyolabs.io");
        Assert.Null(provider.PreflightReject(new Dictionary<string, object?> { ["hd"] = "lyolabs.io" }));
    }

    private static GoogleOpenIdConnectProvider NewProvider(string? hd = null) =>
        new(MsOptions.Create(new GoogleOptions {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RedirectUri = "https://localhost/callback",
            HostedDomain = hd
        }));

    private static class MsOptions
    {
        public static IOptions<T> Create<T>(T value) where T : class, new() => Microsoft.Extensions.Options.Options.Create(value);
    }
}
