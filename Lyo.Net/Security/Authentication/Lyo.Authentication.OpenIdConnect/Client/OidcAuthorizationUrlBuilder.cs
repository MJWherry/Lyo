using System.Net;
using System.Text;
using Lyo.Authentication.OpenIdConnect.Discovery;
using Lyo.Authentication.OpenIdConnect.Pkce;
using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Exceptions;

namespace Lyo.Authentication.OpenIdConnect.Client;

/// <summary>Builds the IdP <c>/authorize</c> URL for a single provider, embedding PKCE/state/nonce.</summary>
public sealed class OidcAuthorizationUrlBuilder
{
    private readonly OidcDiscoveryCache _discovery;

    /// <summary>Creates a new builder.</summary>
    public OidcAuthorizationUrlBuilder(OidcDiscoveryCache discovery)
    {
        ArgumentHelpers.ThrowIfNull(discovery);
        _discovery = discovery;
    }

    /// <summary>Builds the <c>/authorize</c> URL for the given provider.</summary>
    public async Task<string> BuildAsync(IOpenIdConnectProvider provider, string state, string nonce, PkceCodes pkce, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(provider);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(state);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(nonce);
        ArgumentHelpers.ThrowIfNull(pkce);
        var doc = await _discovery.GetAsync(provider.DiscoveryUrl, ct).ConfigureAwait(false);
        var qs = new Dictionary<string, string>(StringComparer.Ordinal) {
            ["client_id"] = provider.ClientId,
            ["redirect_uri"] = provider.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = string.Join(" ", provider.Scopes),
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = pkce.Challenge,
            ["code_challenge_method"] = PkceCodes.Method
        };

        foreach (var kv in provider.ExtraAuthorizeParameters)
            qs[kv.Key] = kv.Value;

        var sb = new StringBuilder(doc.AuthorizationEndpoint);
        sb.Append(doc.AuthorizationEndpoint.IndexOf('?') >= 0 ? '&' : '?');
        var first = true;
        foreach (var kv in qs) {
            if (!first)
                sb.Append('&');

            sb.Append(WebUtility.UrlEncode(kv.Key));
            sb.Append('=');
            sb.Append(WebUtility.UrlEncode(kv.Value));
            first = false;
        }

        return sb.ToString();
    }
}