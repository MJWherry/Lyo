using Lyo.Authentication.Keycloak;

namespace Lyo.Authentication.Keycloak.Tests;

public sealed class KeycloakProviderTests
{
    [Fact]
    public void Provider_HasNameKeycloakColonRealm()
    {
        var provider = NewProvider("acme");
        Assert.Equal("keycloak:acme", provider.Name);
        Assert.Equal("https://sso.lyolabs.io/realms/acme/.well-known/openid-configuration", provider.DiscoveryUrl);
        Assert.Equal(["openid", "email", "profile", "roles"], provider.Scopes);
    }

    [Fact]
    public void Provider_MultipleRealms_AreDistinct()
    {
        var acme = NewProvider("acme");
        var beta = NewProvider("beta");
        Assert.NotEqual(acme.Name, beta.Name);
        Assert.NotEqual(acme.DiscoveryUrl, beta.DiscoveryUrl);
    }

    [Fact]
    public void Provider_HonorsExplicitName()
    {
        var provider = new KeycloakOpenIdConnectProvider(
            new KeycloakOptions {
                BaseUrl = "https://x",
                Realm = "acme",
                ClientId = "c",
                ClientSecret = "s",
                RedirectUri = "https://callback",
                Name = "custom-name"
            });

        Assert.Equal("custom-name", provider.Name);
    }

    [Fact]
    public void Provider_TrimsBaseUrlTrailingSlash()
    {
        var provider = new KeycloakOpenIdConnectProvider(
            new KeycloakOptions {
                BaseUrl = "https://sso.lyolabs.io/",
                Realm = "acme",
                ClientId = "c",
                ClientSecret = "s",
                RedirectUri = "https://callback"
            });

        Assert.Equal("https://sso.lyolabs.io/realms/acme/.well-known/openid-configuration", provider.DiscoveryUrl);
    }

    [Fact]
    public void PreflightReject_AlwaysAllows()
    {
        var provider = NewProvider("acme");
        Assert.Null(provider.PreflightReject(new Dictionary<string, object?>()));
    }

    private static KeycloakOpenIdConnectProvider NewProvider(string realm)
        => new(
            new KeycloakOptions {
                BaseUrl = "https://sso.lyolabs.io",
                Realm = realm,
                ClientId = "client-id",
                ClientSecret = "client-secret",
                RedirectUri = "https://localhost/callback",
                RolesToScopes = new Dictionary<string, string[]> { ["lyo-admin"] = ["admin"] }
            });
}
