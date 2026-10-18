namespace Lyo.Authentication.Sso.Protocol;

/// <summary>Where to send the browser, plus an optional sealed value for the OpenID Connect state cookie.</summary>
/// <param name="RedirectUrl">Absolute URL of the identity provider.</param>
/// <param name="StateCookieValue">Sealed PKCE state when the protocol uses a cookie. Null for SAML.</param>
public sealed record SsoLoginStart(string RedirectUrl, string? StateCookieValue);
