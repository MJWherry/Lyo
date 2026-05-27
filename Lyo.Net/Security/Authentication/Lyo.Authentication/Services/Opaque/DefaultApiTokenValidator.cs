using Lyo.Authentication.Audit;
using Lyo.Authentication.Format;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Options;
using Lyo.Authentication.Records;
using Lyo.Authentication.Services.Users;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Services.Opaque;

/// <summary>
/// Default <see cref="IApiTokenValidator"/>. Parses → store lookup → ring check → hash compare → expiry/revocation check → user-disabled check. Returns the principal on success or
/// <c>null</c> on any failure. Best-effort touches <see cref="ApiTokenRecord.LastUsedAt"/> on success without awaiting.
/// </summary>
public sealed class DefaultApiTokenValidator : IApiTokenValidator
{
    private readonly IApiTokenStore _store;
    private readonly IUserStore _users;
    private readonly AuthenticationOptions _options;
    private readonly IAuthAuditRecorder _audit;
    private readonly IAuthAuditContextAccessor _auditContext;
    private readonly ILogger<DefaultApiTokenValidator> _logger;

    /// <summary>Creates a new validator.</summary>
    public DefaultApiTokenValidator(
        IApiTokenStore store,
        IUserStore users,
        IOptions<AuthenticationOptions> options,
        ILogger<DefaultApiTokenValidator> logger,
        IAuthAuditRecorder? audit = null,
        IAuthAuditContextAccessor? auditContext = null)
    {
        ArgumentHelpers.ThrowIfNull(store);
        ArgumentHelpers.ThrowIfNull(users);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(logger);
        _store = store;
        _users = users;
        _options = options.Value;
        _logger = logger;
        _audit = audit ?? NullAuthAuditRecorder.Instance;
        _auditContext = auditContext ?? NullAuthAuditContextAccessor.Instance;
    }

    /// <inheritdoc/>
    public async Task<ApiTokenPrincipal?> ValidateAsync(string presentedToken, CancellationToken ct = default)
    {
        if (!ApiTokenCodec.TryParse(presentedToken, out var parsed) || parsed is null) {
            _logger.LogDebug("Token rejected: MalformedToken");
            return null;
        }

        if (!string.Equals(parsed.Ring, _options.Ring, StringComparison.Ordinal)) {
            _logger.LogDebug("Token {TokenId} rejected: RingMismatch (presented={Presented}, expected={Expected})", parsed.Id, parsed.Ring, _options.Ring);
            return null;
        }

        var record = await _store.GetByIdAsync(parsed.Id, ct).ConfigureAwait(false);
        if (record is null) {
            _logger.LogDebug("Token rejected: UnknownToken (id={TokenId})", parsed.Id);
            return null;
        }

        if (!string.Equals(record.Kind, parsed.Kind, StringComparison.Ordinal)) {
            _logger.LogDebug("Token {TokenId} rejected: KindMismatch", parsed.Id);
            return null;
        }

        if (!string.Equals(record.Ring, parsed.Ring, StringComparison.Ordinal)) {
            _logger.LogDebug("Token {TokenId} rejected: RingMismatch (record)", parsed.Id);
            return null;
        }

        var presentedHash = ApiTokenCodec.ComputeSecretHash(parsed.Secret);
        if (!FixedTimeEquals(presentedHash, record.SecretHash)) {
            _logger.LogDebug("Token {TokenId} rejected: SecretMismatch", parsed.Id);
            return null;
        }

        var now = DateTime.UtcNow;
        if (record.IsRevoked(now)) {
            _logger.LogDebug("Token {TokenId} rejected: Revoked at {RevokedAt}", parsed.Id, record.RevokedAt);
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.TokenRejected, userId: record.UserId, subject: record.Id, outcome: "failure", reason: "Revoked", ct: ct).ConfigureAwait(false);
            return null;
        }

        if (record.IsExpired(now)) {
            _logger.LogDebug("Token {TokenId} rejected: Expired at {ExpiresAt}", parsed.Id, record.ExpiresAt);
            await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.TokenRejected, userId: record.UserId, subject: record.Id, outcome: "failure", reason: "Expired", ct: ct).ConfigureAwait(false);
            return null;
        }

        var scopes = (IReadOnlyList<string>)record.Scopes;
        if (record.UserId.HasValue) {
            var user = await _users.GetByIdAsync(record.UserId.Value, ct).ConfigureAwait(false);
            if (user is null) {
                _logger.LogDebug("Token {TokenId} rejected: owning user {UserId} not found", parsed.Id, record.UserId);
                await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.TokenRejected, userId: record.UserId, subject: record.Id, outcome: "failure", reason: "OwnerMissing", ct: ct).ConfigureAwait(false);
                return null;
            }

            if (user.IsDisabled) {
                _logger.LogDebug("Token {TokenId} rejected: UserDisabled (user={UserId})", parsed.Id, user.Id);
                await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.TokenRejected, userId: user.Id, subject: record.Id, outcome: "failure", reason: "UserDisabled", ct: ct).ConfigureAwait(false);
                return null;
            }

            if (_options.EnableDynamicScopeIntersection) {
                var current = new HashSet<string>(user.Scopes, StringComparer.Ordinal);
                var intersected = new List<string>(record.Scopes.Count);
                foreach (var s in record.Scopes) {
                    if (current.Contains(s))
                        intersected.Add(s);
                }

                scopes = intersected;
            }
        }

        _ = TouchLastUsedAsync(record.Id, now);
        return new(
            TokenId: record.Id,
            Subject: $"lyo_token:{record.Id}",
            OwnerUserId: record.UserId,
            Kind: record.Kind,
            Ring: record.Ring,
            Scopes: scopes,
            ValidatedAt: now);
    }

    private async Task TouchLastUsedAsync(string id, DateTime now)
    {
        try {
            await _store.TouchLastUsedAsync(id, now, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "Best-effort touch of last_used_at failed for token {TokenId}", id);
        }
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
#if NET10_0_OR_GREATER
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(left, right);
#else
        if (left is null || right is null || left.Length != right.Length)
            return false;

        var accumulator = 0;
        for (var i = 0; i < left.Length; i++)
            accumulator |= left[i] ^ right[i];

        return accumulator == 0;
#endif
    }
}
