using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.OpenIdConnect.Coordinator;

/// <summary>Orchestrates the OIDC callback: code exchange → id_token validation → user provision/link → JWT mint.</summary>
public interface IExternalLoginCoordinator
{
    /// <summary>
    /// Handles the OIDC callback for <paramref name="providerName" />. Returns the freshly minted Lyo JWT and the post-login return target captured at
    /// <see cref="BuildLoginRedirectAsync" /> time.
    /// </summary>
    /// <exception cref="ExternalLoginRejectedException">When state/nonce/signature/policy rejects the login.</exception>
    Task<ExternalLoginResult> HandleCallbackAsync(string providerName, string code, string sealedState, string returnedState, CancellationToken ct = default);

    /// <summary>Generates the IdP <c>/authorize</c> URL plus the matching sealed state cookie value.</summary>
    /// <param name="providerName">Registered <see cref="Provider.IOpenIdConnectProvider" /> name (e.g. <c>google</c>, <c>keycloak:lyo</c>).</param>
    /// <param name="returnUrl">
    /// Pre-validated return target (relative path or allow-listed absolute URL). Sealed into the state cookie verbatim and echoed back to the caller after the
    /// callback succeeds.
    /// </param>
    /// <param name="mode">Token delivery after callback. <c>browser</c> (default) = handoff-code redirect; <c>api</c> = JSON response.</param>
    /// <param name="ct">Cancellation.</param>
    Task<ExternalLoginRedirect> BuildLoginRedirectAsync(string providerName, string returnUrl, string mode = "browser", CancellationToken ct = default);
}

/// <summary>Result of <see cref="IExternalLoginCoordinator.BuildLoginRedirectAsync" />.</summary>
/// <param name="AuthorizeUrl">Where to send the browser.</param>
/// <param name="SealedState">Set this opaque value as a short-lived HttpOnly cookie; present it back to <see cref="IExternalLoginCoordinator.HandleCallbackAsync" />.</param>
public sealed record ExternalLoginRedirect(string AuthorizeUrl, string SealedState);

/// <summary>Result of <see cref="IExternalLoginCoordinator.HandleCallbackAsync" />.</summary>
/// <param name="Issued">Freshly minted Lyo JWT (plus optional rotating refresh token).</param>
/// <param name="ReturnUrl">Return target captured at login start (relative path or allow-listed absolute URL).</param>
/// <param name="Mode">Delivery mode captured at login start (<c>browser</c> or <c>api</c>).</param>
/// <param name="Provider">Provider that issued the login.</param>
public sealed record ExternalLoginResult(IssuedLyoJwt Issued, string ReturnUrl, string Mode, string Provider);