namespace Lyo.Authentication.Sso.Protocol;

/// <summary>One external-login protocol (OpenID Connect or SAML) registered for a set of provider names.</summary>
public interface ISsoProtocol
{
    /// <summary>Stable protocol name (<c>oidc</c> or <c>saml</c>).</summary>
    string Name { get; }

    /// <summary>HTTP method this protocol accepts on <c>/auth/callback/{provider}</c>.</summary>
    SsoCallbackMethod CallbackMethod { get; }

    /// <summary>Provider names this protocol owns. Snapshotted when <see cref="SsoProtocolRegistry" /> is constructed.</summary>
    IReadOnlyCollection<string> ProviderNames { get; }

    /// <summary>Builds the identity-provider redirect for <paramref name="providerName" />.</summary>
    Task<SsoLoginStart> StartAsync(string providerName, string returnUrl, string mode, CancellationToken ct = default);

    /// <summary>Validates <paramref name="callback" />, completes the login once, and returns the minted token plus the stored return target.</summary>
    Task<ExternalLoginResult> CompleteAsync(SsoCallback callback, CancellationToken ct = default);
}
