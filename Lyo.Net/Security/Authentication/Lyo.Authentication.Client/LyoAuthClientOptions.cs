namespace Lyo.Authentication.Client;

/// <summary>Configuration for the consumer-side Lyo auth runtime. <see cref="AuthBaseUrl" /> is the only required field; the rest sensibly default for a web BFF (Gateway-style) host.</summary>
public sealed class LyoAuthClientOptions
{
    /// <summary>Configuration section name (<c>LyoAuthClient</c>).</summary>
    public const string SectionName = "LyoAuthClient";

    /// <summary>
    /// The default authentication scheme name registered by
    /// <see cref="Extensions.AddLyoAuthClient(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{LyoAuthClientOptions})" />.
    /// </summary>
    public const string SchemeName = "LyoAuthCookie";

    /// <summary>Absolute base URL of the Lyo API that hosts the OIDC endpoints (e.g. <c>http://localhost:5251</c>). Required. Used for server-side HTTP (handoff redeem, refresh).</summary>
    public string AuthBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Browser-facing absolute base URL of the API's OIDC endpoints (e.g. <c>https://app.example.com/api</c>). Used when 302ing the user to <c>/auth/login/{provider}</c>. When
    /// empty, falls back to <see cref="AuthBaseUrl" /> (fine for local single-host). In Docker/compose, keep <see cref="AuthBaseUrl" /> as the internal service URL and set this to the
    /// public reverse-proxy URL.
    /// </summary>
    public string? PublicAuthBaseUrl { get; set; }

    /// <summary>Path on this consumer that processes the <c>?lyo_handoff=...</c> redirect from the API. Default <c>/auth/handoff</c>.</summary>
    public string HandoffCallbackPath { get; set; } = "/auth/handoff";

    /// <summary>Path on this consumer that initiates a sign-in (302s the browser to the API's <c>/auth/login/{provider}</c>). Default <c>/auth/sign-in</c>.</summary>
    public string SignInPath { get; set; } = "/auth/sign-in";

    /// <summary>Path on this consumer that signs the user out, revokes the refresh token at the API, and clears the local session cookie. Default <c>/auth/sign-out</c>.</summary>
    public string SignOutPath { get; set; } = "/auth/sign-out";

    /// <summary>Where to send the user after sign-out completes (relative path on this consumer). Default <c>/</c>.</summary>
    public string PostSignOutRedirectPath { get; set; } = "/";

    /// <summary>Name of the HttpOnly session cookie set on this consumer's origin. Default <c>lyo_session</c>.</summary>
    public string CookieName { get; set; } = "lyo_session";

    /// <summary>Optional cookie <c>Domain</c> attribute. <c>null</c> (default) scopes to the request's host. Set this only if you intentionally want the cookie to span subdomains.</summary>
    public string? CookieDomain { get; set; }

    /// <summary>How long the session cookie survives in the browser. Independent of the underlying refresh-token lifetime. Default 30 days.</summary>
    public TimeSpan SessionAbsoluteExpiration { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Grace window applied to access-token expiry before <see cref="LyoAuthDelegatingHandler" /> refreshes pre-emptively. Default 30 seconds.</summary>
    public TimeSpan AccessTokenSkew { get; set; } = TimeSpan.FromSeconds(30);
}