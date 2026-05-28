namespace Lyo.Authentication.OpenIdConnect.Pkce;

/// <summary>The per-login state sealed into the <c>lyo_oidc_state</c> cookie. Survives the round trip to the IdP.</summary>
/// <param name="Verifier">The PKCE verifier (re-presented to the token endpoint).</param>
/// <param name="Nonce">The OIDC nonce (re-checked against the id_token claim).</param>
/// <param name="Provider">The provider name (so we can route the callback even if the URL is hand-typed).</param>
/// <param name="ReturnUrl">
/// Where to redirect the browser after success. Either an absolute URL whose origin was on the BFF allow-list at login time, or a relative path starting with
/// <c>/</c>.
/// </param>
/// <param name="State">
/// The opaque <c>state</c> value sent to the IdP and required to be echoed back. Constant-time compared against the callback's <c>state</c> query parameter to
/// bind the callback to this exact login session.
/// </param>
/// <param name="Mode">
/// How the callback should deliver the issued tokens to the caller. <c>browser</c> (default) drops a single-use handoff code in the redirect URL; <c>api</c>
/// returns tokens as JSON with no redirect.
/// </param>
public sealed record PkceState(string Verifier, string Nonce, string Provider, string ReturnUrl, string State, string Mode = "browser");