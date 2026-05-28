using Lyo.Authentication.OpenIdConnect.Provider;

namespace Lyo.Authentication.OpenIdConnect.Tests;

/// <summary>A minimal in-test <see cref="IOpenIdConnectProvider" /> wired to <see cref="FakeOidcIdentityProvider" />.</summary>
internal sealed class FakeOidcProviderProfile : IOpenIdConnectProvider
{
    public string? RejectIfClaim { get; set; }

    public string Name { get; set; } = "fake";

    public string DiscoveryUrl { get; set; } = FakeOidcIdentityProvider.DiscoveryUrl;

    public string ClientId { get; set; } = "test-client";

    public string ClientSecret { get; set; } = "test-secret";

    public string RedirectUri { get; set; } = "https://localhost/callback";

    public IReadOnlyList<string> Scopes { get; set; } = ["openid", "email", "profile"];

    public IReadOnlyDictionary<string, string> ExtraAuthorizeParameters { get; set; } = new Dictionary<string, string>();

    public OidcClaimMappingResult MapClaims(IReadOnlyDictionary<string, object?> claims)
    {
        var name = (claims.TryGetValue("name", out var n) ? n?.ToString() : null) ?? "unknown";
        var email = claims.TryGetValue("email", out var e) ? e?.ToString() : null;
        var verifiedRaw = claims.TryGetValue("email_verified", out var ev) ? ev : null;
        var verified = verifiedRaw is bool b ? b : true;
        var picture = claims.TryGetValue("picture", out var p) ? p?.ToString() : null;
        var locale = claims.TryGetValue("locale", out var l) ? l?.ToString() : null;
        return new(name, email, verified, picture, locale, []);
    }

    public string? PreflightReject(IReadOnlyDictionary<string, object?> claims)
        => RejectIfClaim is not null && claims.ContainsKey(RejectIfClaim) ? $"rejected via {RejectIfClaim}" : null;
}