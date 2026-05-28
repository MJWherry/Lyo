using System.Text.Json;

namespace Lyo.Authentication.Keycloak.Tests;

public sealed class KeycloakClaimMapperTests
{
    private static readonly Dictionary<string, string[]> Mapping = new() { ["lyo-admin"] = ["admin"], ["lyo-people-rw"] = ["people.read", "people.write"] };

    [Fact]
    public void Map_ExtractsCanonicalClaims_AndAppliesRoleMapping()
    {
        var realmAccess = JsonSerializer.Deserialize<JsonElement>("""{ "roles": ["lyo-admin", "lyo-people-rw", "unmapped"] }""");
        var claims = new Dictionary<string, object?> {
            ["sub"] = "kc-1",
            ["name"] = "Alice",
            ["preferred_username"] = "alice",
            ["email"] = "alice@example.com",
            ["email_verified"] = true,
            ["realm_access"] = realmAccess
        };

        var result = KeycloakClaimMapper.Map(claims, Mapping);
        Assert.Equal("Alice", result.DisplayName);
        Assert.Equal("alice@example.com", result.Email);
        Assert.True(result.EmailVerified);
        Assert.Contains("admin", result.ProviderScopes);
        Assert.Contains("people.read", result.ProviderScopes);
        Assert.Contains("people.write", result.ProviderScopes);
        Assert.DoesNotContain("unmapped", result.ProviderScopes);
    }

    [Fact]
    public void Map_FallsBackToPreferredUsername_WhenNameMissing()
    {
        var result = KeycloakClaimMapper.Map(new Dictionary<string, object?> { ["preferred_username"] = "bob", ["email"] = "bob@example.com" }, Mapping);
        Assert.Equal("bob", result.DisplayName);
    }

    [Fact]
    public void Map_FallsBackToEmail_WhenNameAndUsernameMissing()
    {
        var result = KeycloakClaimMapper.Map(new Dictionary<string, object?> { ["email"] = "carol@example.com" }, Mapping);
        Assert.Equal("carol@example.com", result.DisplayName);
    }

    [Fact]
    public void Map_HandlesDictionaryShapedRealmAccess()
    {
        var realmAccess = new Dictionary<string, object?> { ["roles"] = new object?[] { "lyo-admin" } };
        var result = KeycloakClaimMapper.Map(new Dictionary<string, object?> { ["sub"] = "x", ["realm_access"] = realmAccess }, Mapping);
        Assert.Contains("admin", result.ProviderScopes);
    }

    [Fact]
    public void Map_EmptyMapping_ProducesEmptyScopes()
    {
        var realmAccess = JsonSerializer.Deserialize<JsonElement>("""{ "roles": ["lyo-admin"] }""");
        var result = KeycloakClaimMapper.Map(new Dictionary<string, object?> { ["sub"] = "x", ["realm_access"] = realmAccess }, new Dictionary<string, string[]>());
        Assert.Empty(result.ProviderScopes);
    }

    [Fact]
    public void Map_MissingRealmAccess_ProducesEmptyScopes()
    {
        var result = KeycloakClaimMapper.Map(new Dictionary<string, object?> { ["sub"] = "x" }, Mapping);
        Assert.Empty(result.ProviderScopes);
    }

    [Fact]
    public void ExtractScopes_Deduplicates()
    {
        var realmAccess = JsonSerializer.Deserialize<JsonElement>("""{ "roles": ["lyo-people-rw", "lyo-people-rw"] }""");
        var scopes = KeycloakClaimMapper.ExtractScopes(new Dictionary<string, object?> { ["realm_access"] = realmAccess }, Mapping);
        Assert.Equal(2, scopes.Count);
    }
}