using Lyo.Authentication.Audit;
using Lyo.Authentication.Exceptions;
using Lyo.Authentication.Format;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Options;
using Lyo.Authentication.Services.Users;
using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lyo.Exceptions.Models;

namespace Lyo.Authentication.Services.Opaque;

/// <summary>
/// Default <see cref="IApiTokenIssuer" />. Mints via <see cref="ApiTokenCodec.Mint" />, persists via <see cref="IApiTokenStore" />, retries up to a few times on id
/// collisions, and refuses to issue for disabled users.
/// </summary>
public sealed class DefaultApiTokenIssuer : IApiTokenIssuer
{
    private const int MaxIdCollisionRetries = 5;
    private readonly IAuthAuditRecorder _audit;
    private readonly IAuthAuditContextAccessor _auditContext;
    private readonly ILogger<DefaultApiTokenIssuer> _logger;
    private readonly AuthenticationOptions _options;

    private readonly IApiTokenStore _store;
    private readonly IUserStore _users;

    /// <summary>Creates a new issuer.</summary>
    public DefaultApiTokenIssuer(
        IApiTokenStore store,
        IUserStore users,
        IOptions<AuthenticationOptions> options,
        ILogger<DefaultApiTokenIssuer> logger,
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

    /// <inheritdoc />
    public async Task<IssuedApiToken> IssueAsync(ApiTokenIssueRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Kind);
        ArgumentHelpers.ThrowIfNull(request.Scopes);
        if (request.UserId.HasValue) {
            var user = await _users.GetByIdAsync(request.UserId.Value, null, ct).ConfigureAwait(false);
            if (user is null)
                throw new NotFoundException($"Cannot issue token for unknown user '{request.UserId.Value}'.");

            if (user.IsDisabled)
                throw new LyoUserDisabledException(user.Id, user.DisabledReason);
        }

        var ring = request.Ring.IsNullOrWhitespace() ? _options.Ring : request.Ring;
        var now = DateTime.UtcNow;
        DateTime? expiresAt = null;
        if (request.Lifetime is { } lifetime) {
            if (lifetime > TimeSpan.Zero)
                expiresAt = now + lifetime;
        }
        else if (request.Kind == ApiTokenKind.Pat && _options.DefaultPatLifetime > TimeSpan.Zero)
            expiresAt = now + _options.DefaultPatLifetime;

        var lastException = default(Exception);
        for (var attempt = 0; attempt < MaxIdCollisionRetries; attempt++) {
            var (plaintext, id, secretHash) = ApiTokenCodec.Mint(request.Kind, ring);
            var record = new ApiTokenRecord(
                id, secretHash, request.Kind, ring, request.UserId, request.DisplayName, request.Scopes, request.Metadata, now, null, expiresAt, null, null, null,
                request.RotatedFromId);

            try {
                await _store.InsertAsync(record, null, ct).ConfigureAwait(false);
                _logger.LogInformation("Issued Lyo {Kind}/{Ring} token {TokenId} for user {UserId}", request.Kind, ring, id, request.UserId);
                await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.TokenIssued, request.UserId, id, outcome: "success", reason: request.Kind, ct: ct)
                    .ConfigureAwait(false);

                return new(plaintext, record);
            }
            catch (Exception ex) when (IsDuplicateKey(ex)) {
                lastException = ex;
                _logger.LogWarning(ex, "Token id collision on attempt {Attempt} for kind {Kind}, retrying", attempt + 1, request.Kind);
            }
        }

        throw new ConflictException($"Could not issue a Lyo token after {MaxIdCollisionRetries} attempts (id collisions).", lastException);
    }

    private static bool IsDuplicateKey(Exception ex)
    {
        var message = ex.Message;
        return ex is ConflictException || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) || message.Contains("23505", StringComparison.Ordinal) ||
            ex.GetType().Name.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) ||
            (ex is InvalidOperationException && message.Contains("already exists", StringComparison.OrdinalIgnoreCase));
    }
}