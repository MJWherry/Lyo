using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Options;
using Lyo.Authentication.Services.Users;
using Lyo.Exceptions;
using Lyo.Keystore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Lyo.Authentication.Services.Jwt;

/// <summary>
/// Default <see cref="ILyoJwtValidator" />. Splits header/payload/signature, verifies the signature against the keystore key for the JWT's <c>kid</c>, checks <c>iss</c>/
/// <c>aud</c>/<c>exp</c>/<c>iat</c>, rejects <c>alg=none</c> / non-EdDSA algorithms, and consults <see cref="IUserStore" /> for the Option C user-disabled kill switch.
/// </summary>
public sealed class Ed25519LyoJwtValidator : ILyoJwtValidator
{
    private readonly AuthenticationOptions _authOptions;
    private readonly IKeyStore _keys;
    private readonly ILogger<Ed25519LyoJwtValidator> _logger;
    private readonly LyoJwtOptions _options;
    private readonly IUserStore _users;

    /// <summary>Creates a new validator.</summary>
    public Ed25519LyoJwtValidator(
        IKeyStore keys,
        IUserStore users,
        IOptions<LyoJwtOptions> options,
        IOptions<AuthenticationOptions> authOptions,
        ILogger<Ed25519LyoJwtValidator> logger)
    {
        ArgumentHelpers.ThrowIfNull(keys);
        ArgumentHelpers.ThrowIfNull(users);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(authOptions);
        ArgumentHelpers.ThrowIfNull(logger);
        _keys = keys;
        _users = users;
        _options = options.Value;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ClaimsPrincipal?> ValidateAsync(string jwt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return null;

        var parts = jwt.Split('.');
        if (parts.Length != 3) {
            _logger.LogDebug("JWT rejected: MalformedToken (parts={Count})", parts.Length);
            return null;
        }

        Dictionary<string, JsonElement>? header, payload;
        byte[] signature;
        try {
            header = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Base64Url.Decode(parts[0]));
            payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Base64Url.Decode(parts[1]));
            signature = Base64Url.Decode(parts[2]);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "JWT rejected: MalformedToken (decode)");
            return null;
        }

        if (header is null || payload is null)
            return null;

        if (!header.TryGetValue("alg", out var algEl) || !string.Equals(algEl.GetString(), _options.Algorithm, StringComparison.Ordinal)) {
            _logger.LogDebug("JWT rejected: algorithm mismatch (expected {Expected})", _options.Algorithm);
            return null;
        }

        if (!header.TryGetValue("kid", out var kidEl) || kidEl.ValueKind != JsonValueKind.String) {
            _logger.LogDebug("JWT rejected: missing kid");
            return null;
        }

        var kid = kidEl.GetString()!;
        var (signingKeyId, version) = SplitKid(kid);
        if (!string.Equals(signingKeyId, _options.SigningKeyId, StringComparison.Ordinal)) {
            _logger.LogDebug("JWT rejected: unknown signing key id '{Kid}'", kid);
            return null;
        }

        var seed = await _keys.GetKeyAsync(signingKeyId, version, ct).ConfigureAwait(false);
        if (seed is null) {
            _logger.LogDebug("JWT rejected: unknown signing key version '{Kid}'", kid);
            return null;
        }

        if (seed.Length != Ed25519Constants.PrivateSeedLength) {
            _logger.LogWarning("Signing key '{Kid}' has unexpected length {Length}", kid, seed.Length);
            return null;
        }

        var signingInput = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
        if (!VerifyEd25519(seed, signingInput, signature)) {
            _logger.LogDebug("JWT rejected: signature verification failed");
            return null;
        }

        if (!payload.TryGetValue(LyoJwtClaims.Issuer, out var issEl) || !string.Equals(issEl.GetString(), _options.Issuer, StringComparison.Ordinal)) {
            _logger.LogDebug("JWT rejected: issuer mismatch");
            return null;
        }

        if (!payload.TryGetValue(LyoJwtClaims.Audience, out var audEl) || !string.Equals(audEl.GetString(), _options.Audience, StringComparison.Ordinal)) {
            _logger.LogDebug("JWT rejected: audience mismatch");
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (!TryGetUnixSeconds(payload, LyoJwtClaims.ExpiresAt, out var exp) || now > exp + _options.ClockSkew) {
            _logger.LogDebug("JWT rejected: expired");
            return null;
        }

        if (TryGetUnixSeconds(payload, LyoJwtClaims.IssuedAt, out var iat) && iat > now + _options.ClockSkew) {
            _logger.LogDebug("JWT rejected: iat in the future");
            return null;
        }

        if (TryGetUnixSeconds(payload, LyoJwtClaims.NotBefore, out var nbf) && now + _options.ClockSkew < nbf) {
            _logger.LogDebug("JWT rejected: nbf in the future");
            return null;
        }

        if (!payload.TryGetValue(LyoJwtClaims.LyoUser, out var userIdEl) || !Guid.TryParse(userIdEl.GetString(), out var userId)) {
            _logger.LogDebug("JWT rejected: missing or malformed lyo:user");
            return null;
        }

        var user = await _users.GetByIdAsync(userId, null, ct).ConfigureAwait(false);
        if (user is null) {
            _logger.LogDebug("JWT rejected: user {UserId} not found", userId);
            return null;
        }

        if (user.IsDisabled) {
            _logger.LogDebug("JWT rejected: user {UserId} is disabled", userId);
            return null;
        }

        var claims = new List<Claim>();
        foreach (var kv in payload) {
            if (kv.Key == LyoJwtClaims.Scope) {
                var raw = kv.Value.GetString() ?? string.Empty;
                foreach (var s in raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    claims.Add(new(LyoJwtClaims.Scope, s));

                continue;
            }

            var stringValue = kv.Value.ValueKind switch {
                JsonValueKind.String => kv.Value.GetString(),
                JsonValueKind.Number => kv.Value.ToString(),
                JsonValueKind.True or JsonValueKind.False => kv.Value.GetBoolean().ToString(),
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                var _ => kv.Value.GetRawText()
            };

            if (stringValue is not null)
                claims.Add(new(kv.Key, stringValue));
        }

        if (_authOptions.EnableDynamicScopeIntersection) {
            var current = new HashSet<string>(user.Scopes, StringComparer.Ordinal);
            claims = claims.Where(c => c.Type != LyoJwtClaims.Scope || current.Contains(c.Value)).ToList();
        }

        var identity = new ClaimsIdentity(claims, "LyoJwt", LyoJwtClaims.LyoUser, LyoJwtClaims.Scope);
        return new(identity);
    }

    private static (string KeyId, string Version) SplitKid(string kid)
    {
        var idx = kid.LastIndexOf(':');
        return idx <= 0 ? (kid, "v1") : (kid.Substring(0, idx), kid.Substring(idx + 1));
    }

    private static bool VerifyEd25519(byte[] privateSeed, byte[] data, byte[] signature)
    {
        if (signature.Length != Ed25519Constants.SignatureLength)
            return false;

        var privateKey = new Ed25519PrivateKeyParameters(privateSeed, 0);
        var publicKey = privateKey.GeneratePublicKey();
        var verifier = new Ed25519Signer();
        verifier.Init(false, publicKey);
        verifier.BlockUpdate(data, 0, data.Length);
        return verifier.VerifySignature(signature);
    }

    private static bool TryGetUnixSeconds(Dictionary<string, JsonElement> payload, string key, out DateTimeOffset value)
    {
        value = default;
        if (!payload.TryGetValue(key, out var el))
            return false;

        if (el.ValueKind != JsonValueKind.Number || !el.TryGetInt64(out var seconds))
            return false;

        value = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return true;
    }
}