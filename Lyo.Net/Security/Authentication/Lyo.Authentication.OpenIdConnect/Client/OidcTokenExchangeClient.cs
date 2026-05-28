using System.Net.Http.Json;
using Lyo.Authentication.OpenIdConnect.Discovery;
using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Exceptions;

namespace Lyo.Authentication.OpenIdConnect.Client;

/// <summary>Typed HTTP client that exchanges an authorization code for an id_token / access_token at the provider's <c>/token</c> endpoint.</summary>
public sealed class OidcTokenExchangeClient
{
    private readonly OidcDiscoveryCache _discovery;
    private readonly HttpClient _http;

    /// <summary>Creates a new client.</summary>
    public OidcTokenExchangeClient(HttpClient http, OidcDiscoveryCache discovery)
    {
        ArgumentHelpers.ThrowIfNull(http);
        ArgumentHelpers.ThrowIfNull(discovery);
        _http = http;
        _discovery = discovery;
    }

    /// <summary>POSTs the authorization-code grant to <paramref name="provider" />'s token endpoint with PKCE verifier.</summary>
    public async Task<OidcTokenResponse> ExchangeAsync(IOpenIdConnectProvider provider, string code, string verifier, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(provider);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(code);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(verifier);
        var doc = await _discovery.GetAsync(provider.DiscoveryUrl, ct).ConfigureAwait(false);
        var form = new Dictionary<string, string>(StringComparer.Ordinal) {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = provider.RedirectUri,
            ["client_id"] = provider.ClientId,
            ["client_secret"] = provider.ClientSecret,
            ["code_verifier"] = verifier
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, doc.TokenEndpoint) { Content = new FormUrlEncodedContent(form) };
        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<OidcTokenResponse>(ct).ConfigureAwait(false);
        return tokens ?? throw new InvalidOperationException("Provider /token returned no body.");
    }
}