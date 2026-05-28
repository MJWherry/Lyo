using Lyo.Authentication.OpenIdConnect.Provider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.Keycloak.Tests;

public sealed class KeycloakExtensionsTests
{
    [Fact]
    public void AddKeycloakProviderFromConfiguration_BindsSection()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    ["KeycloakAuth:BaseUrl"] = "https://sso.lyolabs.io",
                    ["KeycloakAuth:Realm"] = "lyo",
                    ["KeycloakAuth:ClientId"] = "lyo-api",
                    ["KeycloakAuth:ClientSecret"] = "secret",
                    ["KeycloakAuth:RedirectUri"] = "https://api/callback",
                    ["KeycloakAuth:RolesToScopes:lyo-admin:0"] = "admin",
                    ["KeycloakAuth:RolesToScopes:lyo-people-rw:0"] = "people.read",
                    ["KeycloakAuth:RolesToScopes:lyo-people-rw:1"] = "people.write"
                })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddKeycloakProviderFromConfiguration(configuration);
        var sp = services.BuildServiceProvider();
        var provider = (KeycloakOpenIdConnectProvider)sp.GetRequiredService<IOpenIdConnectProvider>();
        Assert.Equal("keycloak:lyo", provider.Name);
        Assert.Equal("https://sso.lyolabs.io/realms/lyo/.well-known/openid-configuration", provider.DiscoveryUrl);
    }
}