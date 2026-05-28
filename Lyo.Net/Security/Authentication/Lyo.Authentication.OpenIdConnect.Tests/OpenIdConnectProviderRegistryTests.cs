using Lyo.Authentication.OpenIdConnect.Provider;

namespace Lyo.Authentication.OpenIdConnect.Tests;

public sealed class OpenIdConnectProviderRegistryTests
{
    [Fact]
    public void Get_ReturnsRegisteredProvider()
    {
        var provider = new TestProvider("google");
        var registry = new OpenIdConnectProviderRegistry([provider]);
        Assert.Same(provider, registry.Get("google"));
    }

    [Fact]
    public void Get_ThrowsForUnknown()
    {
        var registry = new OpenIdConnectProviderRegistry([]);
        Assert.Throws<InvalidOperationException>(() => registry.Get("nope"));
    }

    [Fact]
    public void Register_RejectsDuplicates()
    {
        var registry = new OpenIdConnectProviderRegistry([new TestProvider("google")]);
        Assert.Throws<InvalidOperationException>(() => registry.Register(new TestProvider("google")));
    }

    [Fact]
    public void TryGet_ReturnsNullForUnknown()
    {
        var registry = new OpenIdConnectProviderRegistry([]);
        Assert.Null(registry.TryGet("unknown"));
    }

    private sealed class TestProvider(string name) : IOpenIdConnectProvider
    {
        public string Name => name;

        public string DiscoveryUrl => "https://example/.well-known/openid-configuration";

        public string ClientId => "c";

        public string ClientSecret => "s";

        public string RedirectUri => "https://localhost/cb";

        public IReadOnlyList<string> Scopes { get; } = ["openid"];

        public IReadOnlyDictionary<string, string> ExtraAuthorizeParameters { get; } = new Dictionary<string, string>();

        public OidcClaimMappingResult MapClaims(IReadOnlyDictionary<string, object?> claims) => new("name", "email@x", true, null, null, []);

        public string? PreflightReject(IReadOnlyDictionary<string, object?> claims) => null;
    }
}