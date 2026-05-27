using System;

namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>Configuration for the WASM-side Lyo auth runtime. <see cref="AuthBaseUrl"/> is the only required field.</summary>
public sealed class WasmAuthClientOptions
{
    /// <summary>Configuration section name (<c>LyoAuthWasmClient</c>).</summary>
    public const string SectionName = "LyoAuthWasmClient";

    /// <summary>Absolute base URL of the Lyo API hosting the OIDC endpoints (e.g. <c>https://api.example.com</c>). Required. The API must include this client's origin in <c>LyoOidcBff.AllowedReturnOrigins</c>.</summary>
    public string AuthBaseUrl { get; set; } = string.Empty;

    /// <summary>Path on this WASM app that the API will redirect the browser back to after a successful external login. Default <c>/auth/handoff</c>.</summary>
    public string HandoffCallbackPath { get; set; } = "/auth/handoff";

    /// <summary>Where to send the user after sign-out completes (relative path on this WASM app). Default <c>/</c>.</summary>
    public string PostSignOutRedirectPath { get; set; } = "/";

    /// <summary>LocalStorage key under which the session snapshot is persisted. Default <c>lyo_auth_session</c>.</summary>
    public string StorageKey { get; set; } = "lyo_auth_session";

    /// <summary>Grace window applied to access-token expiry before <c>WasmAuthDelegatingHandler</c> refreshes pre-emptively. Default 30 seconds.</summary>
    public TimeSpan AccessTokenSkew { get; set; } = TimeSpan.FromSeconds(30);
}
