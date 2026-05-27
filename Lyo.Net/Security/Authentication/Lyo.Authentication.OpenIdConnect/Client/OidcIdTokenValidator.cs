using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Format;
using Lyo.Authentication.OpenIdConnect.Discovery;
using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Lyo.Authentication.OpenIdConnect.Client;

/// <summary>
/// Validates an id_token: parses header/payload, looks up the signing key via <see cref="OidcJwksResolver"/>, verifies the signature (RS256 / RS384 / RS512 / EdDSA / ES256 / ES384), and
/// checks <c>iss</c>, <c>aud</c>, <c>nonce</c>, <c>exp</c>, <c>iat</c>, and <c>nbf</c>.
/// </summary>
public sealed class OidcIdTokenValidator
{
    /// <summary>Default clock skew when comparing <c>exp</c>/<c>iat</c>/<c>nbf</c>.</summary>
    public static readonly TimeSpan DefaultClockSkew = TimeSpan.FromSeconds(60);

    private readonly OidcDiscoveryCache _discovery;
    private readonly OidcJwksResolver _jwks;
    private readonly ILogger<OidcIdTokenValidator> _logger;
    private readonly TimeSpan _clockSkew;

    /// <summary>Creates a new validator.</summary>
    public OidcIdTokenValidator(OidcDiscoveryCache discovery, OidcJwksResolver jwks, ILogger<OidcIdTokenValidator> logger, TimeSpan? clockSkew = null)
    {
        ArgumentHelpers.ThrowIfNull(discovery);
        ArgumentHelpers.ThrowIfNull(jwks);
        ArgumentHelpers.ThrowIfNull(logger);
        _discovery = discovery;
        _jwks = jwks;
        _logger = logger;
        _clockSkew = clockSkew ?? DefaultClockSkew;
    }

    /// <summary>Validates the id_token and returns its claims when valid, or <c>null</c> on any failure.</summary>
    public async Task<IReadOnlyDictionary<string, object?>?> ValidateAsync(IOpenIdConnectProvider provider, string idToken, string expectedNonce, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(provider);
        if (string.IsNullOrWhiteSpace(idToken))
            return null;

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(expectedNonce);
        var parts = idToken.Split('.');
        if (parts.Length != 3) {
            _logger.LogDebug("id_token rejected: malformed (parts={Count})", parts.Length);
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
            _logger.LogDebug(ex, "id_token rejected: decode failure");
            return null;
        }

        if (header is null || payload is null)
            return null;

        var alg = header.TryGetValue("alg", out var algEl) ? algEl.GetString() : null;
        var kid = header.TryGetValue("kid", out var kidEl) ? kidEl.GetString() : null;
        if (string.IsNullOrEmpty(alg) || string.Equals(alg, "none", StringComparison.OrdinalIgnoreCase)) {
            _logger.LogDebug("id_token rejected: alg={Alg}", alg);
            return null;
        }

        if (string.IsNullOrEmpty(kid)) {
            _logger.LogDebug("id_token rejected: missing kid");
            return null;
        }

        var discovery = await _discovery.GetAsync(provider.DiscoveryUrl, ct).ConfigureAwait(false);
        var key = await _jwks.ResolveAsync(discovery.JwksUri, kid!, ct).ConfigureAwait(false);
        if (key is null) {
            _logger.LogDebug("id_token rejected: unknown kid {Kid}", kid);
            return null;
        }

        var signingInput = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
        if (!VerifySignature(alg!, key, signingInput, signature)) {
            _logger.LogDebug("id_token rejected: signature");
            return null;
        }

        if (!payload.TryGetValue("iss", out var issEl) || !string.Equals(issEl.GetString(), discovery.Issuer, StringComparison.Ordinal)) {
            _logger.LogDebug("id_token rejected: iss mismatch (expected {Expected})", discovery.Issuer);
            return null;
        }

        if (!AudienceMatches(payload, provider.ClientId, out var multiAud)) {
            _logger.LogDebug("id_token rejected: aud mismatch");
            return null;
        }

        if (multiAud) {
            if (!payload.TryGetValue("azp", out var azpEl) || !string.Equals(azpEl.GetString(), provider.ClientId, StringComparison.Ordinal)) {
                _logger.LogDebug("id_token rejected: multi-audience token without matching azp");
                return null;
            }
        }

        if (!payload.TryGetValue("nonce", out var nonceEl) || !string.Equals(nonceEl.GetString(), expectedNonce, StringComparison.Ordinal)) {
            _logger.LogDebug("id_token rejected: nonce mismatch");
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (!TryGetUnixSeconds(payload, "exp", out var exp) || now > exp + _clockSkew) {
            _logger.LogDebug("id_token rejected: expired");
            return null;
        }

        if (TryGetUnixSeconds(payload, "iat", out var iat) && iat > now + _clockSkew) {
            _logger.LogDebug("id_token rejected: iat in future");
            return null;
        }

        if (TryGetUnixSeconds(payload, "nbf", out var nbf) && now + _clockSkew < nbf) {
            _logger.LogDebug("id_token rejected: nbf in future");
            return null;
        }

        var claims = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kv in payload)
            claims[kv.Key] = ToClaimValue(kv.Value);

        var preflight = provider.PreflightReject(claims);
        if (preflight is not null) {
            _logger.LogDebug("id_token rejected by provider preflight: {Reason}", preflight);
            return null;
        }

        return claims;
    }

    private static bool AudienceMatches(Dictionary<string, JsonElement> payload, string expected, out bool multipleAudiences)
    {
        multipleAudiences = false;
        if (!payload.TryGetValue("aud", out var audEl))
            return false;

        if (audEl.ValueKind == JsonValueKind.String)
            return string.Equals(audEl.GetString(), expected, StringComparison.Ordinal);

        if (audEl.ValueKind != JsonValueKind.Array)
            return false;

        var count = 0;
        var matched = false;
        foreach (var el in audEl.EnumerateArray()) {
            if (el.ValueKind != JsonValueKind.String)
                continue;

            count++;
            if (string.Equals(el.GetString(), expected, StringComparison.Ordinal))
                matched = true;
        }

        multipleAudiences = count > 1;
        return matched;
    }

    private static bool VerifySignature(string alg, OidcJsonWebKey key, byte[] data, byte[] signature) =>
        alg switch {
            "RS256" => VerifyRsa(key, data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            "RS384" => VerifyRsa(key, data, signature, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
            "RS512" => VerifyRsa(key, data, signature, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
            "PS256" => VerifyRsa(key, data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
            "PS384" => VerifyRsa(key, data, signature, HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
            "PS512" => VerifyRsa(key, data, signature, HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
            "ES256" => VerifyEcdsa(key, data, signature, HashAlgorithmName.SHA256),
            "ES384" => VerifyEcdsa(key, data, signature, HashAlgorithmName.SHA384),
            "EdDSA" => VerifyEd25519(key, data, signature),
            _ => false
        };

    private static bool VerifyRsa(OidcJsonWebKey key, byte[] data, byte[] signature, HashAlgorithmName hash, RSASignaturePadding padding)
    {
        if (!string.Equals(key.Kty, "RSA", StringComparison.Ordinal) || string.IsNullOrEmpty(key.N) || string.IsNullOrEmpty(key.E))
            return false;

        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters {
            Modulus = Base64Url.Decode(key.N!),
            Exponent = Base64Url.Decode(key.E!)
        });

        return rsa.VerifyData(data, signature, hash, padding);
    }

    private static bool VerifyEcdsa(OidcJsonWebKey key, byte[] data, byte[] signature, HashAlgorithmName hash)
    {
        if (!string.Equals(key.Kty, "EC", StringComparison.Ordinal) || string.IsNullOrEmpty(key.X) || string.IsNullOrEmpty(key.Y) || string.IsNullOrEmpty(key.Crv))
            return false;

        ECCurve curve = key.Crv switch {
            "P-256" => ECCurve.NamedCurves.nistP256,
            "P-384" => ECCurve.NamedCurves.nistP384,
            _ => default
        };

        if (curve.Oid is null)
            return false;

        using var ecdsa = ECDsa.Create(new ECParameters {
            Curve = curve,
            Q = new() {
                X = Base64Url.Decode(key.X!),
                Y = Base64Url.Decode(key.Y!)
            }
        });

        return ecdsa.VerifyData(data, signature, hash);
    }

    private static bool VerifyEd25519(OidcJsonWebKey key, byte[] data, byte[] signature)
    {
        if (!string.Equals(key.Kty, "OKP", StringComparison.Ordinal) || !string.Equals(key.Crv, "Ed25519", StringComparison.Ordinal) || string.IsNullOrEmpty(key.X))
            return false;

        var pub = new Ed25519PublicKeyParameters(Base64Url.Decode(key.X!), 0);
        var signer = new Ed25519Signer();
        signer.Init(false, pub);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.VerifySignature(signature);
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

    private static object? ToClaimValue(JsonElement el) =>
        el.ValueKind switch {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var n) ? n : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Array or JsonValueKind.Object => el.Clone(),
            _ => el.GetRawText()
        };
}
