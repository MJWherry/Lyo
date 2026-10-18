using Lyo.Authentication.Audit;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Sso.Completion;
using Lyo.Authentication.Sso.Protocol;
using Lyo.Common.Core.Security;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.Saml;

/// <summary>SAML 2.0 service-provider protocol. Login is an HTTP-Redirect AuthnRequest. Completion is an HTTP-POST to the assertion consumer.</summary>
public sealed class SamlSsoProtocol(
    IEnumerable<ISamlIdentityProvider> providers,
    ISamlMetadataSource metadata,
    ISamlLoginTicketStore tickets,
    ISamlAssertionReplayCache replay,
    SamlResponseValidator validator,
    IExternalLoginCompletion completion,
    IAuthAuditRecorder audit,
    IAuthAuditContextAccessor auditContext,
    ILogger<SamlSsoProtocol> logger) : ISsoProtocol
{

    /// <inheritdoc />
    public string Name => "saml";

    /// <inheritdoc />
    public SsoCallbackMethod CallbackMethod => SsoCallbackMethod.Post;

    /// <inheritdoc />
    public IReadOnlyCollection<string> ProviderNames => providers.Select(provider => provider.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();

    /// <inheritdoc />
    public async Task<SsoLoginStart> StartAsync(string providerName, string returnUrl, string mode, CancellationToken ct = default)
    {
        var provider = Require(providerName);
        var idp = await metadata.GetAsync(provider, ct).ConfigureAwait(false);
        var requestId = "_" + Base64Url.Encode(CryptographicRandom.GetBytes(16));
        var xml = SamlAuthnRequest.Write(requestId, DateTime.UtcNow, provider.SpEntityId, idp.RedirectSsoUrl, provider.AcsUrl);
        var ticket = tickets.Issue(provider.Name, requestId, returnUrl, "browser");
        var redirect = SamlAuthnRequest.RedirectUrl(idp.RedirectSsoUrl, SamlAuthnRequest.Deflate(xml), ticket.Id);
        return new SsoLoginStart(redirect, null);
    }

    /// <inheritdoc />
    public async Task<ExternalLoginResult> CompleteAsync(SsoCallback callback, CancellationToken ct = default)
    {
        try
        {
            return await CompleteInnerAsync(callback, ct).ConfigureAwait(false);
        }
        catch (ExternalLoginRejectedException ex)
        {
            await audit.RecordAsync(auditContext, logger, AuthAuditEventKind.ExternalLoginRejected, provider: callback.Provider, outcome: "failure", reason: ex.Reason, ct: ct)
                .ConfigureAwait(false);
            throw;
        }
    }

    private async Task<ExternalLoginResult> CompleteInnerAsync(SsoCallback callback, CancellationToken ct)
    {
        var relayState = Value(callback.Form, "RelayState");
        var samlResponse = Value(callback.Form, "SAMLResponse");
        if (string.IsNullOrWhiteSpace(relayState) || string.IsNullOrWhiteSpace(samlResponse))
            throw new ExternalLoginRejectedException("SamlTicket", "The SAML response is missing a login ticket.");

        var ticket = tickets.Consume(relayState);
        if (ticket is null || !string.Equals(ticket.Provider, callback.Provider, StringComparison.Ordinal))
            throw new ExternalLoginRejectedException("SamlTicket", "The SAML login ticket is missing, expired, or for a different provider.");

        var provider = Require(callback.Provider);
        var idp = await metadata.GetAsync(provider, ct).ConfigureAwait(false);
        var assertion = validator.Validate(samlResponse, ticket, provider, idp, DateTime.UtcNow, replay);
        var issued = await completion.CompleteAsync(assertion, ct).ConfigureAwait(false);
        return new ExternalLoginResult(issued, ticket.ReturnUrl, "browser", provider.Name);
    }

    private ISamlIdentityProvider Require(string providerName)
    {
        var match = providers.FirstOrDefault(provider => string.Equals(provider.Name, providerName, StringComparison.Ordinal));
        if (match is null)
            throw new NotFoundException($"SAML provider '{providerName}' is not registered.");
        return match;
    }

    private static string? Value(IReadOnlyDictionary<string, string?> form, string key)
        => form.TryGetValue(key, out var value) ? value : null;
}
