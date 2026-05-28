using Lyo.Authentication.Web.Components.Models;

namespace Lyo.Authentication.Web.Components.Abstractions;

/// <summary>
/// Optional username + password sign-in strategy. Not registered by default — the BFF / API stack has no password grant today, so this is a placeholder contract for apps
/// that want to bolt local password auth onto the same login page. When no implementation is registered the password form is omitted entirely.
/// </summary>
public interface IAuthPasswordSignIn
{
    /// <summary>Attempts to sign in with the supplied credentials. Implementations must never throw on bad credentials — return <see cref="AuthPasswordSignInResult.Failure" /> instead.</summary>
    /// <param name="username">User-entered username, email, or other identifier.</param>
    /// <param name="password">User-entered password. Cleared by the UI after submission either way.</param>
    /// <param name="rememberMe">User's "remember me" preference; impls free to ignore.</param>
    /// <param name="returnUrl">Optional post-login destination — passed through on success.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AuthPasswordSignInResult> SignInAsync(string username, string password, bool rememberMe, string? returnUrl, CancellationToken ct = default);
}