using Lyo.Authentication.Audit;
using Lyo.Authentication.Format;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Services.Jwt;
using Lyo.Authentication.Services.Opaque;
using Lyo.Authentication.Services.Users;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.Services.Refresh;

/// <summary>Default <see cref="ILyoRefreshTokenExchange" />. Validates → revokes (rotates) → issues new JWT + refresh.</summary>
public sealed class DefaultLyoRefreshTokenExchange : ILyoRefreshTokenExchange
{
    private readonly IAuthAuditRecorder _audit;
    private readonly IAuthAuditContextAccessor _auditContext;
    private readonly IExternalIdentityStore? _identities;
    private readonly ILyoJwtIssuer _jwtIssuer;
    private readonly ILogger<DefaultLyoRefreshTokenExchange> _logger;
    private readonly IApiTokenStore _store;
    private readonly IUserStore _users;
    private readonly IApiTokenValidator _validator;

    /// <summary>Creates a new exchange.</summary>
    public DefaultLyoRefreshTokenExchange(
        IApiTokenValidator validator,
        IApiTokenStore store,
        IUserStore users,
        ILyoJwtIssuer jwtIssuer,
        ILogger<DefaultLyoRefreshTokenExchange> logger,
        IExternalIdentityStore? identities = null,
        IAuthAuditRecorder? audit = null,
        IAuthAuditContextAccessor? auditContext = null)
    {
        ArgumentHelpers.ThrowIfNull(validator);
        ArgumentHelpers.ThrowIfNull(store);
        ArgumentHelpers.ThrowIfNull(users);
        ArgumentHelpers.ThrowIfNull(jwtIssuer);
        ArgumentHelpers.ThrowIfNull(logger);
        _validator = validator;
        _store = store;
        _users = users;
        _jwtIssuer = jwtIssuer;
        _logger = logger;
        _identities = identities;
        _audit = audit ?? NullAuthAuditRecorder.Instance;
        _auditContext = auditContext ?? NullAuthAuditContextAccessor.Instance;
    }

    /// <inheritdoc />
    public async Task<IssuedLyoJwt?> ExchangeAsync(string presentedRefreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(presentedRefreshToken))
            return null;

        if (!ApiTokenCodec.TryParse(presentedRefreshToken, out var parsed) || parsed is null) {
            _logger.LogDebug("Refresh exchange rejected: malformed token");
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.RefreshRejected, outcome: "failure", reason: "Malformed", ct: ct).ConfigureAwait(false);
            return null;
        }

        if (!string.Equals(parsed.Kind, ApiTokenKind.Internal, StringComparison.Ordinal)) {
            _logger.LogDebug("Refresh exchange rejected: token {TokenId} is not internal kind", parsed.Id);
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.RefreshRejected, subject: parsed.Id, outcome: "failure", reason: "KindMismatch", ct: ct)
                .ConfigureAwait(false);

            return null;
        }

        var principal = await _validator.ValidateAsync(presentedRefreshToken, ct).ConfigureAwait(false);
        if (principal is null) {
            await HandlePossibleTheftAsync(parsed.Id, ct).ConfigureAwait(false);
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.RefreshRejected, subject: parsed.Id, outcome: "failure", reason: "ValidationFailed", ct: ct)
                .ConfigureAwait(false);

            return null;
        }

        var hasRefreshScope = false;
        foreach (var s in principal.Scopes) {
            if (string.Equals(s, LyoRefreshTokenScopes.Refresh, StringComparison.Ordinal)) {
                hasRefreshScope = true;
                break;
            }
        }

        if (!hasRefreshScope) {
            _logger.LogDebug("Refresh exchange rejected: token {TokenId} missing {Scope}", parsed.Id, LyoRefreshTokenScopes.Refresh);
            await _audit.RecordAsync(
                    _auditContext, _logger, AuthAuditEventKind.RefreshRejected, principal.OwnerUserId, parsed.Id, outcome: "failure", reason: "MissingScope", ct: ct)
                .ConfigureAwait(false);

            return null;
        }

        if (!principal.OwnerUserId.HasValue) {
            _logger.LogDebug("Refresh exchange rejected: token {TokenId} has no owner user", parsed.Id);
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.RefreshRejected, subject: parsed.Id, outcome: "failure", reason: "NoOwner", ct: ct)
                .ConfigureAwait(false);

            return null;
        }

        var user = await _users.GetByIdAsync(principal.OwnerUserId.Value, null, ct).ConfigureAwait(false);
        if (user is null || user.IsDisabled) {
            _logger.LogDebug("Refresh exchange rejected: owner user disabled or missing");
            await _audit.RecordAsync(
                    _auditContext, _logger, AuthAuditEventKind.RefreshRejected, principal.OwnerUserId, parsed.Id, outcome: "failure",
                    reason: user is null ? "UserNotFound" : "UserDisabled", ct: ct)
                .ConfigureAwait(false);

            return null;
        }

        var record = await _store.GetByIdAsync(parsed.Id, null, ct).ConfigureAwait(false);
        var (provider, externalSubject) = ExtractProvider(record);
        var effectiveScopes = await ResolveEffectiveScopesAsync(user, provider, externalSubject, ct).ConfigureAwait(false);
        await _store.RevokeAsync(parsed.Id, DateTime.UtcNow, "rotated", null, ct).ConfigureAwait(false);
        await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.TokenRevoked, user.Id, parsed.Id, provider, "success", "rotated", ct: ct).ConfigureAwait(false);
        var issued = await _jwtIssuer.IssueAsync(user, effectiveScopes, provider, externalSubject, true, ct).ConfigureAwait(false);
        await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.RefreshSucceeded, user.Id, issued.AccessTokenJti, provider, "success", ct: ct).ConfigureAwait(false);
        return issued;
    }

    private async Task<IReadOnlyList<string>> ResolveEffectiveScopesAsync(LyoUser user, string provider, string? externalSubject, CancellationToken ct)
    {
        if (_identities is null || string.IsNullOrWhiteSpace(externalSubject) || string.Equals(provider, "local", StringComparison.Ordinal))
            return user.Scopes;

        try {
            var link = await _identities.FindByProviderSubjectAsync(provider, externalSubject!, null, ct).ConfigureAwait(false);
            if (link is null || link.UserId != user.Id || !link.IsActive)
                return user.Scopes;

            if (user.Scopes.Count == 0)
                return link.Scopes;

            if (link.Scopes.Count == 0)
                return user.Scopes;

            var union = new HashSet<string>(user.Scopes, StringComparer.Ordinal);
            foreach (var s in link.Scopes)
                union.Add(s);

            return union.ToArray();
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "Failed to resolve linked identity for refresh rotation; falling back to user scopes only");
            return user.Scopes;
        }
    }

    private static (string Provider, string? ExternalSubject) ExtractProvider(ApiTokenRecord? record)
    {
        if (record?.Metadata is null)
            return ("local", null);

        var provider = record.Metadata.TryGetValue(DefaultLyoRefreshTokenIssuer.ProviderMetadataKey, out var pRaw) ? pRaw?.ToString() : null;
        var subject = record.Metadata.TryGetValue(DefaultLyoRefreshTokenIssuer.ExternalSubjectMetadataKey, out var sRaw) ? sRaw?.ToString() : null;
        return (string.IsNullOrWhiteSpace(provider) ? "local" : provider!, string.IsNullOrWhiteSpace(subject) ? null : subject);
    }

    private async Task HandlePossibleTheftAsync(string tokenId, CancellationToken ct)
    {
        try {
            var record = await _store.GetByIdAsync(tokenId, null, ct).ConfigureAwait(false);
            if (record is null || !record.RevokedAt.HasValue)
                return;

            _logger.LogWarning(
                "Refresh exchange rejected: presented an already-revoked refresh token {TokenId} (theft detection). Original revocation reason: {Reason}", tokenId,
                record.RevokedReason);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "Refresh-exchange theft detection failed");
        }
    }
}