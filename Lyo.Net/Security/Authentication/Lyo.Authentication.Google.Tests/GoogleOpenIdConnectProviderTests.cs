using Lyo.Authentication.Google;

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
        => Assert.Throws<ArgumentException>(()
            => new GoogleOpenIdConnectProvider(new GoogleOptions { ClientId = string.Empty, ClientSecret = "secret", RedirectUri = "https://x" }));

    [Fact]
    public void PreflightReject_AllowsWhenHostedDomainUnset()
    {
        var provider = NewProvider();
        Assert.Null(provider.PreflightReject(new Dictionary<string, object?>()));
    }

    [Fact]
    public void PreflightReject_RejectsPersonalAccountWhenHostedDomainSet()
    {
        var provider = NewProvider("lyolabs.io");
        var reason = provider.PreflightReject(new Dictionary<string, object?>());
        Assert.Equal("HostedDomainMismatch", reason);
    }

    [Fact]
    public void PreflightReject_RejectsWrongHostedDomain()
    {
        var provider = NewProvider("lyolabs.io");
        var reason = provider.PreflightReject(new Dictionary<string, object?> { ["hd"] = "nope.io" });
        Assert.Equal("HostedDomainMismatch", reason);
    }

    [Fact]
    public void PreflightReject_AcceptsCorrectHostedDomain()
    {
        var provider = NewProvider("lyolabs.io");
        Assert.Null(provider.PreflightReject(new Dictionary<string, object?> { ["hd"] = "lyolabs.io" }));
    }

    private static GoogleOpenIdConnectProvider NewProvider(string? hd = null)
        => new(
            new GoogleOptions {
                ClientId = "client-id",
                ClientSecret = "client-secret",
                RedirectUri = "https://localhost/callback",
                HostedDomain = hd
            });
}
