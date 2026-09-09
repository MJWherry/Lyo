using System.Text;
using System.Text.Json;
using Lyo.Authentication.Audit;
using Lyo.Authentication.Exceptions;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Options;
using Lyo.Authentication.Services.Refresh;
using Lyo.Authentication.Services.Users;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Core.Security;
using Lyo.Exceptions;
using Lyo.KeyStore;
using Lyo.KeyStore.Exceptions;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Lyo.Authentication.Services.Jwt;

/// <summary>Default <see cref="ILyoJwtIssuer" /> backed by BouncyCastle's Ed25519 signer. Always pulls the *current* signing key from <see cref="IKeyStore" />.</summary>
public sealed class Ed25519LyoJwtIssuer : ILyoJwtIssuer
{
    private readonly IAuthAuditRecorder _audit;
    private readonly IAuthAuditContextAccessor _auditContext;
    private readonly IUserClaimStore? _claims;
    private readonly IUserScopeStore? _scopes;
    private readonly IKeyStore _keys;
    private readonly ILogger<Ed25519LyoJwtIssuer> _logger;
    private readonly LyoJwtOptions _options;
    private readonly ILyoRefreshTokenIssuer? _refreshIssuer;

    /// <summary>Builds a new issuer.</summary>
    public Ed25519LyoJwtIssuer(
        IKeyStore keys,
        LyoJwtOptions options,
        ILogger<Ed25519LyoJwtIssuer> logger,
        ILyoRefreshTokenIssuer? refreshIssuer = null,
        IUserClaimStore? claims = null,
        IUserScopeStore? scopes = null,
        IAuthAuditRecorder? audit = null,
        IAuthAuditContextAccessor? auditContext = null)
    {
        ArgumentHelpers.ThrowIfNull(keys);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(logger);
        _keys = keys;
        _options = options;
        _logger = logger;
        _refreshIssuer = refreshIssuer;
        _claims = claims;
        _scopes = scopes;
        _audit = audit ?? NullAuthAuditRecorder.Instance;
        _auditContext = auditContext ?? NullAuthAuditContextAccessor.Instance;
    }

    /// <inheritdoc />
    public async Task<IssuedLyoJwt> IssueAsync(
        LyoUser user,
        IReadOnlyList<string> scopes,
        string provider,
        string? externalSubject,
        bool includeRefresh = true,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(user);
        ArgumentHelpers.ThrowIfNull(scopes);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(provider);
        if (user.IsDisabled)
            throw new LyoUserDisabledException(user.Id, user.DisabledReason);

        var now = DateTime.UtcNow;
        var expires = now + _options.AccessTokenLifetime;
        var version = await _keys.GetCurrentVersionAsync(_options.SigningKeyId, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNullOrWhiteSpace(version, $"No current signing key version found for '{_options.SigningKeyId}'.");
        var seed = await _keys.GetCurrentKeyAsync(_options.SigningKeyId, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(seed, $"No current signing key bytes found for '{_options.SigningKeyId}'.");
        if (seed.Length != Ed25519Constants.PrivateSeedLength)
            throw new InvalidKeyException($"Signing key '{_options.SigningKeyId}' v{version} is {seed.Length} bytes; expected {Ed25519Constants.PrivateSeedLength} for Ed25519.");

        var kid = $"{_options.SigningKeyId}:{version}";
        var jti = Convert.ToBase64String(CryptographicRandom.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var effectiveScopes = scopes;
        if (_scopes is not null) {
            var stored = await _scopes.ListForUserAsync(user.Id, null, ct).ConfigureAwait(false);
            if (stored.Count > 0)
                effectiveScopes = stored.Select(s => s.Name).Distinct(StringComparer.Ordinal).ToArray();
        }

        var header = new Dictionary<string, object> { ["alg"] = _options.Algorithm, ["kid"] = kid, ["typ"] = "JWT" };
        var payload = new Dictionary<string, object?> {
            [LyoJwtClaims.Issuer] = _options.Issuer,
            [LyoJwtClaims.Audience] = _options.Audience,
            [LyoJwtClaims.Subject] = $"lyo_user:{user.Id:D}",
            [LyoJwtClaims.IssuedAt] = ToUnixSeconds(now),
            [LyoJwtClaims.NotBefore] = ToUnixSeconds(now),
            [LyoJwtClaims.ExpiresAt] = ToUnixSeconds(expires),
            [LyoJwtClaims.TokenId] = jti,
            [LyoJwtClaims.LyoUser] = user.Id.ToString("D"),
            [LyoJwtClaims.LyoProvider] = provider,
            [LyoJwtClaims.Scope] = string.Join(" ", effectiveScopes.Distinct(StringComparer.Ordinal))
        };

        if (!externalSubject.IsNullOrWhitespace())
            payload[LyoJwtClaims.LyoExternalSub] = externalSubject;

        if (_claims is not null) {
            var extra = await _claims.ListForUserAsync(user.Id, null, ct).ConfigureAwait(false);
            foreach (var claim in extra) {
                if (LyoJwtClaims.IsReserved(claim.Type) || payload.ContainsKey(claim.Type))
                    continue;

                payload[claim.Type] = claim.Value;
            }
        }

        var headerJson = JsonSerializer.SerializeToUtf8Bytes(header);
        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(payload);
        var headerB64 = Base64Url.Encode(headerJson);
        var payloadB64 = Base64Url.Encode(payloadJson);
        var signingInputBytes = Encoding.ASCII.GetBytes($"{headerB64}.{payloadB64}");
        var signature = SignEd25519(seed, signingInputBytes);
        var signatureB64 = Base64Url.Encode(signature);
        var accessToken = $"{headerB64}.{payloadB64}.{signatureB64}";
        string? refreshToken = null;
        DateTime? refreshExpires = null;
        if (includeRefresh && _refreshIssuer is not null) {
            var refresh = await _refreshIssuer.IssueAsync(user.Id, jti, _options.RefreshTokenLifetime, provider, externalSubject, ct).ConfigureAwait(false);
            refreshToken = refresh.Plaintext;
            refreshExpires = refresh.Record.ExpiresAt;
        }

        _logger.LogInformation("Issued Lyo JWT for user {UserId} via provider {Provider} (jti={Jti}, exp={Exp:O})", user.Id, provider, jti, expires);
        await _audit.RecordAsync(_auditContext, _logger, AuthAuditEventKind.JwtIssued, user.Id, jti, provider, "success", ct: ct).ConfigureAwait(false);
        return new(accessToken, jti, expires, refreshToken, refreshExpires);
    }

    private static byte[] SignEd25519(byte[] privateSeed, byte[] data)
    {
        var key = new Ed25519PrivateKeyParameters(privateSeed, 0);
        var signer = new Ed25519Signer();
        signer.Init(true, key);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    private static long ToUnixSeconds(DateTime utc) => (long)(utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
}