using System.Security.Cryptography;
using System.Text;
using Lyo.Authentication.Audit;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.OpenIdConnect.Client;
using Lyo.Authentication.OpenIdConnect.Pkce;
using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Authentication.Sso.Completion;
using Lyo.Authentication.Sso.Protocol;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.OpenIdConnect.Protocol;

/// <summary>OpenID Connect <see cref="ISsoProtocol" />. Validates the code and id_token, then calls SSO completion once.</summary>
public sealed class OidcSsoProtocol : ISsoProtocol
{
    private readonly IAuthAuditRecorder _audit;
    private readonly IAuthAuditContextAccessor _auditContext;
    private readonly OidcAuthorizationUrlBuilder _authzBuilder;
    private readonly IExternalLoginCompletion _completion;
    private readonly OidcTokenExchangeClient _exchange;
    private readonly OidcIdTokenValidator _idTokenValidator;
    private readonly ILogger<OidcSsoProtocol> _logger;
    private readonly OpenIdConnectProviderRegistry _providers;
    private readonly StateNonceProtector _state;

    /// <summary>Builds the OpenID Connect protocol.</summary>
    public OidcSsoProtocol(
        OpenIdConnectProviderRegistry providers,
        OidcAuthorizationUrlBuilder authzBuilder,
        OidcTokenExchangeClient exchange,
        OidcIdTokenValidator idTokenValidator,
        StateNonceProtector state,
        IExternalLoginCompletion completion,
        ILogger<OidcSsoProtocol> logger,
        IAuthAuditRecorder? audit = null,
        IAuthAuditContextAccessor? auditContext = null)
    {
        ArgumentHelpers.ThrowIfNull(providers);
        ArgumentHelpers.ThrowIfNull(authzBuilder);
        ArgumentHelpers.ThrowIfNull(exchange);
        ArgumentHelpers.ThrowIfNull(idTokenValidator);
        ArgumentHelpers.ThrowIfNull(state);
        ArgumentHelpers.ThrowIfNull(completion);
        ArgumentHelpers.ThrowIfNull(logger);
        _providers = providers;
        _authzBuilder = authzBuilder;
        _exchange = exchange;
        _idTokenValidator = idTokenValidator;
        _state = state;
        _completion = completion;
        _logger = logger;
        _audit = audit ?? NullAuthAuditRecorder.Instance;
        _auditContext = auditContext ?? NullAuthAuditContextAccessor.Instance;
    }

    /// <inheritdoc />
    public string Name => "oidc";

    /// <inheritdoc />
    public SsoCallbackMethod CallbackMethod => SsoCallbackMethod.Get;

    /// <inheritdoc />
    public IReadOnlyCollection<string> ProviderNames => _providers.All.Select(p => p.Name).ToArray();

    /// <inheritdoc />
    public async Task<SsoLoginStart> StartAsync(string providerName, string returnUrl, string mode, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentHelpers.ThrowIfNull(returnUrl);
        var normalizedMode = NormalizeMode(mode);
        var provider = _providers.Get(providerName);
        var pkce = PkceCodes.Generate();
        var state = StateNonceProtector.GenerateState();
        var nonce = StateNonceProtector.GenerateNonce();
        var pkceState = new PkceState(pkce.Verifier, nonce, providerName, returnUrl, state, normalizedMode);
        var sealedState = _state.Seal(pkceState);
        var url = await _authzBuilder.BuildAsync(provider, state, nonce, pkce, ct).ConfigureAwait(false);
        return new(url, sealedState);
    }

    /// <inheritdoc />
    public async Task<ExternalLoginResult> CompleteAsync(SsoCallback callback, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(callback);
        try {
            return await CompleteInnerAsync(callback, ct).ConfigureAwait(false);
        }
        catch (ExternalLoginRejectedException ex) {
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.ExternalLoginRejected, provider: callback.Provider, outcome: "failure", reason: ex.Reason, ct: ct)
                .ConfigureAwait(false);

            throw;
        }
    }

    private async Task<ExternalLoginResult> CompleteInnerAsync(SsoCallback callback, CancellationToken ct)
    {
        var providerName = callback.Provider;
        var code = callback.Query.TryGetValue("code", out var codeValue) ? codeValue : null;
        var returnedState = callback.Query.TryGetValue("state", out var stateValue) ? stateValue : null;
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(providerName);
        if (code.IsNullOrWhitespace())
            throw new ExternalLoginRejectedException("OidcStateInvalid", "missing authorization code");

        var provider = _providers.Get(providerName);
        var pkceState = _state.Unseal(callback.StateCookie) ?? throw new ExternalLoginRejectedException("OidcStateInvalid", "missing or tampered state cookie");
        if (!string.Equals(pkceState.Provider, providerName, StringComparison.Ordinal))
            throw new ExternalLoginRejectedException("OidcStateInvalid", "provider mismatch between state cookie and callback");

        if (returnedState.IsNullOrWhitespace())
            throw new ExternalLoginRejectedException("OidcStateInvalid", "missing state on callback");

        if (!FixedTimeStringEquals(pkceState.State, returnedState))
            throw new ExternalLoginRejectedException("OidcStateInvalid", "callback state does not match the value bound to the state cookie");

        var tokens = await _exchange.ExchangeAsync(provider, code, pkceState.Verifier, ct).ConfigureAwait(false);
        if (tokens.IdToken.IsNullOrEmpty())
            throw new ExternalLoginRejectedException("OidcSignatureInvalid", "provider returned no id_token");

        var claims = await _idTokenValidator.ValidateAsync(provider, tokens.IdToken, pkceState.Nonce, ct).ConfigureAwait(false) ??
            throw new ExternalLoginRejectedException("OidcSignatureInvalid", "id_token failed validation");

        var subject = claims.TryGetValue("sub", out var subEl) ? subEl?.ToString() : null;
        if (subject.IsNullOrWhitespace())
            throw new ExternalLoginRejectedException("OidcSignatureInvalid", "id_token has no sub");

        var mapping = provider.MapClaims(claims);
        var assertion = new ExternalLoginAssertion(
            provider.Name, subject, mapping.DisplayName, mapping.Email, mapping.EmailVerified, mapping.AvatarUrl, mapping.PreferredLanguageBcp47, mapping.ProviderScopes, claims);

        var issued = await _completion.CompleteAsync(assertion, ct).ConfigureAwait(false);
        return new(issued, pkceState.ReturnUrl, NormalizeMode(pkceState.Mode), provider.Name);
    }

    private static string NormalizeMode(string? mode) => string.Equals(mode, "api", StringComparison.OrdinalIgnoreCase) ? "api" : "browser";

    private static bool FixedTimeStringEquals(string? a, string? b)
    {
        var ab = Encoding.UTF8.GetBytes(a ?? string.Empty);
        var bb = Encoding.UTF8.GetBytes(b ?? string.Empty);
        if (ab.Length != bb.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(ab, bb);
    }
}
