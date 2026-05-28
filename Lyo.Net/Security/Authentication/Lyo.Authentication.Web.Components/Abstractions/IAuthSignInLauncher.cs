namespace Lyo.Authentication.Web.Components.Abstractions;

/// <summary>
/// Host-side strategy that knows how to start an external (OIDC) sign-in flow and how to sign out the active session. The Server host redirects through the BFF
/// (`/auth/sign-in/{provider}`) while the WASM host navigates directly to the API and reads the handoff back in the browser.
/// </summary>
public interface IAuthSignInLauncher
{
    /// <summary>
    /// Begins an interactive sign-in with the named provider. Implementations typically navigate the browser away from the page, so they should not be awaited for an in-page
    /// result.
    /// </summary>
    /// <param name="provider">Provider name as registered in the API's OIDC registry (e.g. <c>google</c>, <c>keycloak:my-realm</c>).</param>
    /// <param name="returnUrl">Optional local path or allow-listed absolute URL to navigate to once sign-in completes.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SignInAsync(string provider, string? returnUrl, CancellationToken ct = default);

    /// <summary>Signs the active session out. Best-effort: revokes the refresh token at the API and clears the local session state.</summary>
    Task SignOutAsync(CancellationToken ct = default);
}