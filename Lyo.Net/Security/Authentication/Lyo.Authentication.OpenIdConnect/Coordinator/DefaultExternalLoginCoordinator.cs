using Lyo.Authentication.OpenIdConnect.Protocol;
using Lyo.Authentication.Sso.Protocol;
using Lyo.Exceptions;

namespace Lyo.Authentication.OpenIdConnect.Coordinator;

/// <summary>OIDC facade. Login and callback both go through <see cref="OidcSsoProtocol" />, which completes once.</summary>
public sealed class DefaultExternalLoginCoordinator : IExternalLoginCoordinator
{
    private readonly OidcSsoProtocol _protocol;

    /// <summary>Builds the facade.</summary>
    public DefaultExternalLoginCoordinator(OidcSsoProtocol protocol)
    {
        ArgumentHelpers.ThrowIfNull(protocol);
        _protocol = protocol;
    }

    /// <inheritdoc />
    public async Task<ExternalLoginRedirect> BuildLoginRedirectAsync(string providerName, string returnUrl, string mode = "browser", CancellationToken ct = default)
    {
        var start = await _protocol.StartAsync(providerName, returnUrl, mode, ct).ConfigureAwait(false);
        return new(start.RedirectUrl, start.StateCookieValue ?? string.Empty);
    }

    /// <inheritdoc />
    public Task<ExternalLoginResult> HandleCallbackAsync(string providerName, string code, string sealedState, string returnedState, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>(StringComparer.Ordinal) { ["code"] = code, ["state"] = returnedState };
        return _protocol.CompleteAsync(new(providerName, "GET", query, new Dictionary<string, string?>(), sealedState), ct);
    }
}
