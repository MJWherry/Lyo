using System.Diagnostics;

namespace Lyo.Authentication.Sso.Protocol;

/// <summary>Where to send the browser, plus an optional sealed value for the OpenID Connect state cookie.</summary>
/// <param name="RedirectUrl">Absolute URL of the identity provider.</param>
/// <param name="StateCookieValue">Sealed PKCE state when the protocol uses a cookie. Null for SAML.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SsoLoginStart(string RedirectUrl, string? StateCookieValue)
{
    /// <summary>Redirect URL. The state cookie value is omitted.</summary>
    public override string ToString() => $"Redirect={RedirectUrl} StateCookieSet={!string.IsNullOrEmpty(StateCookieValue)}";
}