using System.Diagnostics;
using Lyo.Authentication.Sso.Protocol;

namespace Lyo.Authentication.OpenIdConnect.Coordinator;

/// <summary>Facade over <c>OidcSsoProtocol</c> so existing callers keep <c>BuildLoginRedirectAsync</c> and <c>HandleCallbackAsync</c>.</summary>
public interface IExternalLoginCoordinator
{
    /// <summary>Handles the OpenID Connect callback. The protocol mints the Lyo JWT once.</summary>
    /// <exception cref="Sso.Completion.ExternalLoginRejectedException">When state, nonce, signature, or policy rejects the login.</exception>
    Task<ExternalLoginResult> HandleCallbackAsync(string providerName, string code, string sealedState, string returnedState, CancellationToken ct = default);

    /// <summary>Generates the IdP authorize URL and the sealed state cookie value.</summary>
    Task<ExternalLoginRedirect> BuildLoginRedirectAsync(string providerName, string returnUrl, string mode = "browser", CancellationToken ct = default);
}

/// <summary>Result of <see cref="IExternalLoginCoordinator.BuildLoginRedirectAsync" />.</summary>
/// <param name="AuthorizeUrl">Where to send the browser.</param>
/// <param name="SealedState">HttpOnly cookie value presented back to <see cref="IExternalLoginCoordinator.HandleCallbackAsync" />.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ExternalLoginRedirect(string AuthorizeUrl, string SealedState)
{
    /// <summary>Authorize URL. The sealed state value is omitted.</summary>
    public override string ToString() => $"AuthorizeUrl={AuthorizeUrl} SealedStateSet={SealedState.Length > 0}";
}