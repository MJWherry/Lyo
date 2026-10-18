using Lyo.Authentication.Audit;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Services.Jwt;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Users;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.Sso.Completion;

/// <summary>Default <see cref="IExternalLoginCompletion" />. One call mints one JWT and replaces that provider's refresh tokens.</summary>
public sealed class DefaultExternalLoginCompletion : IExternalLoginCompletion
{
    private readonly IAuthAuditRecorder _audit;
    private readonly IAuthAuditContextAccessor _auditContext;
    private readonly IExternalIdentityStore _identities;
    private readonly ILyoJwtIssuer _jwtIssuer;
    private readonly ILogger<DefaultExternalLoginCompletion> _logger;
    private readonly ExternalLoginOptions _options;
    private readonly IUserScopeStore? _scopes;
    private readonly IApiTokenStore _tokens;
    private readonly IUserStore _users;

    /// <summary>Builds the completion service.</summary>
    public DefaultExternalLoginCompletion(
        IUserStore users,
        IExternalIdentityStore identities,
        IApiTokenStore tokens,
        ILyoJwtIssuer jwtIssuer,
        ExternalLoginOptions options,
        ILogger<DefaultExternalLoginCompletion> logger,
        IAuthAuditRecorder? audit = null,
        IAuthAuditContextAccessor? auditContext = null,
        IUserScopeStore? scopes = null)
    {
        ArgumentHelpers.ThrowIfNull(users);
        ArgumentHelpers.ThrowIfNull(identities);
        ArgumentHelpers.ThrowIfNull(tokens);
        ArgumentHelpers.ThrowIfNull(jwtIssuer);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(logger);
        _users = users;
        _identities = identities;
        _tokens = tokens;
        _jwtIssuer = jwtIssuer;
        _options = options;
        _logger = logger;
        _audit = audit ?? NullAuthAuditRecorder.Instance;
        _auditContext = auditContext ?? NullAuthAuditContextAccessor.Instance;
        _scopes = scopes;
    }

    /// <inheritdoc />
    public async Task<IssuedLyoJwt> CompleteAsync(ExternalLoginAssertion assertion, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(assertion);
        if (_options.RequireVerifiedEmail && !assertion.EmailVerified)
            throw new ExternalLoginRejectedException("EmailNotVerified", $"{assertion.Provider} reported email_verified=false");

        var link = await _identities.FindByProviderSubjectAsync(assertion.Provider, assertion.Subject, null, ct).ConfigureAwait(false);
        LyoUser user;
        var newlyProvisioned = false;
        if (link is null) {
            user = await ResolveOrProvisionAsync(assertion, ct).ConfigureAwait(false);
            await _identities.LinkAsync(user.Id, assertion.Provider, assertion.Subject, assertion.Email, assertion.ProviderScopes, assertion.RawClaims, null, ct).ConfigureAwait(false);
            _logger.LogInformation("Provisioned and linked Lyo user {UserId} for {Provider}", user.Id, assertion.Provider);
            newlyProvisioned = true;
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.UserProvisioned, user.Id, assertion.Subject, assertion.Provider, "success", ct: ct).ConfigureAwait(false);
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.IdentityLinked, user.Id, assertion.Subject, assertion.Provider, "success", ct: ct).ConfigureAwait(false);
        }
        else {
            user = await _users.GetByIdAsync(link.UserId, null, ct).ConfigureAwait(false) ??
                throw new ExternalLoginRejectedException("UserNotProvisioned", "linked Lyo user not found");

            if (user.IsDisabled)
                throw new ExternalLoginRejectedException("UserDisabled", "Lyo user is disabled");

            await _identities.LinkAsync(user.Id, assertion.Provider, assertion.Subject, assertion.Email, assertion.ProviderScopes, assertion.RawClaims, null, ct).ConfigureAwait(false);
        }

        await _users.UpdateLastLoginAsync(user.Id, DateTime.UtcNow, null, ct).ConfigureAwait(false);
        if (_scopes is not null)
            await UserScopeNames.SeedAsync(_scopes, user.Id, user.Scopes, null, ct).ConfigureAwait(false);

        var effective = await UserScopeNames.ResolveAsync(_scopes, user, ct).ConfigureAwait(false);
        await _tokens.RevokeRefreshTokensForUserAsync(user.Id, assertion.Provider, DateTime.UtcNow, "login_replaced", null, ct).ConfigureAwait(false);
        var issued = await _jwtIssuer.IssueAsync(user, effective, assertion.Provider, assertion.Subject, true, ct).ConfigureAwait(false);
        await _audit.RecordAsync(
                _auditContext, _logger, AuthAuditEventKind.ExternalLoginSucceeded, user.Id, assertion.Subject, assertion.Provider, "success", newlyProvisioned ? "jit_provisioned" : null, ct: ct)
            .ConfigureAwait(false);

        return issued;
    }

    private async Task<LyoUser> ResolveOrProvisionAsync(ExternalLoginAssertion assertion, CancellationToken ct)
    {
        switch (_options.Policy) {
            case ExternalLoginPolicy.RequireExistingUser: {
                if (assertion.Email.IsNullOrWhitespace())
                    throw new ExternalLoginRejectedException("UserNotProvisioned", "no email and policy=RequireExistingUser");

                return await _users.GetByEmailAsync(assertion.Email, null, ct).ConfigureAwait(false) ??
                    throw new ExternalLoginRejectedException("UserNotProvisioned", "no pre-existing Lyo user with that email");
            }
            case ExternalLoginPolicy.JitFromAllowedClaim: {
                if (!IsAllowedByClaim(assertion.RawClaims))
                    throw new ExternalLoginRejectedException("UserNotProvisioned", "claim outside allowed set");

                return await ProvisionAsync(assertion, ct).ConfigureAwait(false);
            }
            case ExternalLoginPolicy.JustInTime:
            default:
                return await ProvisionAsync(assertion, ct).ConfigureAwait(false);
        }
    }

    private bool IsAllowedByClaim(IReadOnlyDictionary<string, object?> claims)
    {
        if (_options.AllowedClaimName.IsNullOrWhitespace() || _options.AllowedClaimValues.Count == 0)
            return false;

        if (!claims.TryGetValue(_options.AllowedClaimName, out var raw) || raw is null)
            return false;

        if (raw is string text)
            return Matches(text);

        if (raw is IReadOnlyList<string> list)
            return list.Any(Matches);

        if (raw is IEnumerable<string> values)
            return values.Any(Matches);

        return Matches(raw.ToString());
    }

    private bool Matches(string? value) => value is not null && _options.AllowedClaimValues.Any(v => string.Equals(v, value, StringComparison.Ordinal));

    private async Task<LyoUser> ProvisionAsync(ExternalLoginAssertion assertion, CancellationToken ct)
    {
        if (assertion.Email.IsNullOrWhitespace())
            throw new ExternalLoginRejectedException("UserNotProvisioned", $"{assertion.Provider} returned no email; cannot provision");

        var existing = await _users.GetByEmailAsync(assertion.Email, null, ct).ConfigureAwait(false);
        if (existing is not null)
            return existing;

        var now = DateTime.UtcNow;
        var baselineScopes = _options.DefaultUserScopes.Count == 0 ? Array.Empty<string>() : _options.DefaultUserScopes.Distinct(StringComparer.Ordinal).ToArray();
        var display = assertion.DisplayName.IsNullOrWhitespace() ? assertion.Email : assertion.DisplayName;
        var user = new LyoUser(
            LyoGuid.CreateV7(), display, assertion.Email, assertion.EmailVerified, assertion.AvatarUrl, assertion.PreferredLanguageBcp47, baselineScopes, null, null, now, null, null, null, null);

        return await _users.CreateAsync(user, null, ct).ConfigureAwait(false);
    }
}
