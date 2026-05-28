using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Audit;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.OpenIdConnect.Client;
using Lyo.Authentication.OpenIdConnect.Pkce;
using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Authentication.Services.Jwt;
using Lyo.Authentication.Services.Users;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.OpenIdConnect.Coordinator;

/// <summary>The default <see cref="IExternalLoginCoordinator"/>: orchestrates the BFF callback per the plan's flow diagram.</summary>
public sealed class DefaultExternalLoginCoordinator : IExternalLoginCoordinator
{
    private readonly OpenIdConnectProviderRegistry _providers;
    private readonly OidcAuthorizationUrlBuilder _authzBuilder;
    private readonly OidcTokenExchangeClient _exchange;
    private readonly OidcIdTokenValidator _idTokenValidator;
    private readonly StateNonceProtector _state;
    private readonly IUserStore _users;
    private readonly IExternalIdentityStore _identities;
    private readonly ILyoJwtIssuer _jwtIssuer;
    private readonly ExternalLoginOptions _options;
    private readonly IAuthAuditRecorder _audit;
    private readonly IAuthAuditContextAccessor _auditContext;
    private readonly ILogger<DefaultExternalLoginCoordinator> _logger;

    /// <summary>Creates a new coordinator.</summary>
    public DefaultExternalLoginCoordinator(
        OpenIdConnectProviderRegistry providers,
        OidcAuthorizationUrlBuilder authzBuilder,
        OidcTokenExchangeClient exchange,
        OidcIdTokenValidator idTokenValidator,
        StateNonceProtector state,
        IUserStore users,
        IExternalIdentityStore identities,
        ILyoJwtIssuer jwtIssuer,
        IOptions<ExternalLoginOptions> options,
        ILogger<DefaultExternalLoginCoordinator> logger,
        IAuthAuditRecorder? audit = null,
        IAuthAuditContextAccessor? auditContext = null)
    {
        ArgumentHelpers.ThrowIfNull(providers);
        ArgumentHelpers.ThrowIfNull(authzBuilder);
        ArgumentHelpers.ThrowIfNull(exchange);
        ArgumentHelpers.ThrowIfNull(idTokenValidator);
        ArgumentHelpers.ThrowIfNull(state);
        ArgumentHelpers.ThrowIfNull(users);
        ArgumentHelpers.ThrowIfNull(identities);
        ArgumentHelpers.ThrowIfNull(jwtIssuer);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(logger);
        _providers = providers;
        _authzBuilder = authzBuilder;
        _exchange = exchange;
        _idTokenValidator = idTokenValidator;
        _state = state;
        _users = users;
        _identities = identities;
        _jwtIssuer = jwtIssuer;
        _options = options.Value;
        _logger = logger;
        _audit = audit ?? NullAuthAuditRecorder.Instance;
        _auditContext = auditContext ?? NullAuthAuditContextAccessor.Instance;
    }

    /// <inheritdoc/>
    public async Task<ExternalLoginRedirect> BuildLoginRedirectAsync(string providerName, string returnUrl, string mode = "browser", CancellationToken ct = default)
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

    private static string NormalizeMode(string? mode) =>
        string.Equals(mode, "api", StringComparison.OrdinalIgnoreCase) ? "api" : "browser";

    /// <inheritdoc/>
    public async Task<ExternalLoginResult> HandleCallbackAsync(string providerName, string code, string sealedState, string returnedState, CancellationToken ct = default)
    {
        try {
            return await HandleCallbackInnerAsync(providerName, code, sealedState, returnedState, ct).ConfigureAwait(false);
        }
        catch (ExternalLoginRejectedException ex) {
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.ExternalLoginRejected, provider: providerName, outcome: "failure", reason: ex.Reason, ct: ct).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<ExternalLoginResult> HandleCallbackInnerAsync(string providerName, string code, string sealedState, string returnedState, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(code);
        var provider = _providers.Get(providerName);
        var pkceState = _state.Unseal(sealedState)
            ?? throw new ExternalLoginRejectedException("OidcStateInvalid", "missing or tampered state cookie");

        if (!string.Equals(pkceState.Provider, providerName, StringComparison.Ordinal))
            throw new ExternalLoginRejectedException("OidcStateInvalid", "provider mismatch between state cookie and callback");

        if (string.IsNullOrWhiteSpace(returnedState))
            throw new ExternalLoginRejectedException("OidcStateInvalid", "missing state on callback");

        if (!FixedTimeStringEquals(pkceState.State, returnedState))
            throw new ExternalLoginRejectedException("OidcStateInvalid", "callback state does not match the value bound to the state cookie");

        var tokens = await _exchange.ExchangeAsync(provider, code, pkceState.Verifier, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(tokens.IdToken))
            throw new ExternalLoginRejectedException("OidcSignatureInvalid", "provider returned no id_token");

        var claims = await _idTokenValidator.ValidateAsync(provider, tokens.IdToken!, pkceState.Nonce, ct).ConfigureAwait(false)
            ?? throw new ExternalLoginRejectedException("OidcSignatureInvalid", "id_token failed validation");

        var subject = claims.TryGetValue("sub", out var subEl) ? subEl?.ToString() : null;
        if (string.IsNullOrWhiteSpace(subject))
            throw new ExternalLoginRejectedException("OidcSignatureInvalid", "id_token has no sub");

        var mapping = provider.MapClaims(claims);
        if (_options.RequireVerifiedEmail && !mapping.EmailVerified)
            throw new ExternalLoginRejectedException("EmailNotVerified", $"{provider.Name} reported email_verified=false");

        var link = await _identities.FindByProviderSubjectAsync(provider.Name, subject!, tenantId: null, ct).ConfigureAwait(false);
        LyoUser user;
        var newlyProvisioned = false;
        if (link is null) {
            user = await ResolveOrProvisionAsync(provider, mapping, claims, ct).ConfigureAwait(false);
            link = await _identities.LinkAsync(user.Id, provider.Name, subject!, mapping.Email, mapping.ProviderScopes, claims, tenantId: null, ct).ConfigureAwait(false);
            _logger.LogInformation("Provisioned and linked Lyo user {UserId} for {Provider}", user.Id, provider.Name);
            newlyProvisioned = true;
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.UserProvisioned, userId: user.Id, subject: subject, provider: provider.Name, outcome: "success", ct: ct).ConfigureAwait(false);
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.IdentityLinked, userId: user.Id, subject: subject, provider: provider.Name, outcome: "success", ct: ct).ConfigureAwait(false);
        }
        else {
            user = await _users.GetByIdAsync(link.UserId, tenantId: null, ct).ConfigureAwait(false)
                ?? throw new ExternalLoginRejectedException("UserNotProvisioned", "linked Lyo user not found");

            if (user.IsDisabled)
                throw new ExternalLoginRejectedException("UserDisabled", "Lyo user is disabled");

            link = await _identities.LinkAsync(user.Id, provider.Name, subject!, mapping.Email, mapping.ProviderScopes, claims, tenantId: null, ct).ConfigureAwait(false);
        }

        await _users.UpdateLastLoginAsync(user.Id, DateTime.UtcNow, tenantId: null, ct).ConfigureAwait(false);
        var effective = EffectiveScopes(user, link);
        var issued = await _jwtIssuer.IssueAsync(user, effective, provider.Name, subject, includeRefresh: true, ct).ConfigureAwait(false);
        await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.ExternalLoginSucceeded, userId: user.Id, subject: subject, provider: provider.Name, outcome: "success", reason: newlyProvisioned ? "jit_provisioned" : null, ct: ct).ConfigureAwait(false);
        return new(issued, pkceState.ReturnUrl, NormalizeMode(pkceState.Mode), provider.Name);
    }

    private static bool FixedTimeStringEquals(string a, string b)
    {
        var ab = Encoding.UTF8.GetBytes(a ?? string.Empty);
        var bb = Encoding.UTF8.GetBytes(b ?? string.Empty);
        if (ab.Length != bb.Length)
            return false;

#if NET10_0_OR_GREATER
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ab, bb);
#else
        var accumulator = 0;
        for (var i = 0; i < ab.Length; i++)
            accumulator |= ab[i] ^ bb[i];

        return accumulator == 0;
#endif
    }

    private async Task<LyoUser> ResolveOrProvisionAsync(IOpenIdConnectProvider provider, OidcClaimMappingResult mapping, IReadOnlyDictionary<string, object?> claims, CancellationToken ct)
    {
        switch (_options.Policy) {
            case ExternalLoginPolicy.RequireExistingUser: {
                if (string.IsNullOrWhiteSpace(mapping.Email))
                    throw new ExternalLoginRejectedException("UserNotProvisioned", "no email and policy=RequireExistingUser");

                return await _users.GetByEmailAsync(mapping.Email!, tenantId: null, ct).ConfigureAwait(false)
                    ?? throw new ExternalLoginRejectedException("UserNotProvisioned", "no pre-existing Lyo user with that email");
            }

            case ExternalLoginPolicy.JitFromAllowedClaim: {
                if (!IsAllowedByClaim(claims))
                    throw new ExternalLoginRejectedException("UserNotProvisioned", "claim outside allowed set");

                return await ProvisionAsync(mapping, provider.Name, ct).ConfigureAwait(false);
            }

            case ExternalLoginPolicy.JustInTime:
            default:
                return await ProvisionAsync(mapping, provider.Name, ct).ConfigureAwait(false);
        }
    }

    private bool IsAllowedByClaim(IReadOnlyDictionary<string, object?> claims)
    {
        if (string.IsNullOrWhiteSpace(_options.AllowedClaimName) || _options.AllowedClaimValues.Count == 0)
            return false;

        if (!claims.TryGetValue(_options.AllowedClaimName!, out var raw) || raw is null)
            return false;

        var value = raw.ToString();
        return value is not null && _options.AllowedClaimValues.Any(v => string.Equals(v, value, StringComparison.Ordinal));
    }

    private async Task<LyoUser> ProvisionAsync(OidcClaimMappingResult mapping, string providerName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(mapping.Email))
            throw new ExternalLoginRejectedException("UserNotProvisioned", $"{providerName} returned no email; cannot provision");

        var existing = await _users.GetByEmailAsync(mapping.Email!, tenantId: null, ct).ConfigureAwait(false);
        if (existing is not null)
            return existing;

        var now = DateTime.UtcNow;
        var baselineScopes = _options.DefaultUserScopes.Count == 0
            ? Array.Empty<string>()
            : _options.DefaultUserScopes.Distinct(StringComparer.Ordinal).ToArray();
        var user = new LyoUser(
            Id: Guid.NewGuid(),
            DisplayName: string.IsNullOrWhiteSpace(mapping.DisplayName) ? mapping.Email! : mapping.DisplayName,
            Email: mapping.Email!,
            EmailVerified: mapping.EmailVerified,
            AvatarUrl: mapping.AvatarUrl,
            PreferredLanguageBcp47: mapping.PreferredLanguageBcp47,
            Scopes: baselineScopes,
            Metadata: null,
            PersonId: null,
            CreatedAt: now,
            UpdatedAt: null,
            LastLoginAt: null,
            DisabledAt: null,
            DisabledReason: null);

        return await _users.CreateAsync(user, tenantId: null, ct).ConfigureAwait(false);
    }

    private static IReadOnlyList<string> EffectiveScopes(LyoUser user, LinkedIdentity link)
    {
        if (user.Scopes.Count == 0)
            return link.Scopes;

        if (link.Scopes.Count == 0)
            return user.Scopes;

        var union = new HashSet<string>(user.Scopes, StringComparer.Ordinal);
        foreach (var s in link.Scopes)
            union.Add(s);

        return union.ToArray();
    }
}
