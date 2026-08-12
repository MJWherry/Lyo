using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Options;
using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Lyo.KeyStore;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Parameters;

namespace Lyo.Authentication.Services.Jwt;

/// <summary>
/// Builds the JWKS document served at <c>/.well-known/jwks.json</c>. Publishes every version of the signing key currently in the keystore, so JWTs signed by an older key
/// keep validating until they expire.
/// </summary>
public sealed class JwkSetBuilder
{
    private readonly IKeyStore _keys;
    private readonly LyoJwtOptions _options;

    /// <summary>Creates a new builder.</summary>
    public JwkSetBuilder(IKeyStore keys, IOptions<LyoJwtOptions> options)
    {
        ArgumentHelpers.ThrowIfNull(keys);
        ArgumentHelpers.ThrowIfNull(options);
        _keys = keys;
        _options = options.Value;
    }

    /// <summary>Builds the JWKS payload. Returns a dictionary ready to be serialized as JSON.</summary>
    public async Task<IReadOnlyDictionary<string, object>> BuildAsync(CancellationToken ct = default)
    {
        var keys = new List<Dictionary<string, object>>();
        if (_keys is IKeyInventoryStore inventory) {
            var versions = await inventory.GetAvailableVersionsAsync(_options.SigningKeyId, ct).ConfigureAwait(false);
            foreach (var version in versions.OrderBy(v => v)) {
                var seed = await _keys.GetKeyAsync(_options.SigningKeyId, version, ct).ConfigureAwait(false);
                if (seed is null || seed.Length != Ed25519Constants.PrivateSeedLength)
                    continue;

                keys.Add(BuildJwk(seed, $"{_options.SigningKeyId}:{version}"));
            }
        }
        else {
            var current = await _keys.GetCurrentKeyAsync(_options.SigningKeyId, ct).ConfigureAwait(false);
            var version = await _keys.GetCurrentVersionAsync(_options.SigningKeyId, ct).ConfigureAwait(false);
            if (current is not null && current.Length == Ed25519Constants.PrivateSeedLength && !version.IsNullOrWhitespace())
                keys.Add(BuildJwk(current, $"{_options.SigningKeyId}:{version}"));
        }

        return new Dictionary<string, object> { ["keys"] = keys };
    }

    private static Dictionary<string, object> BuildJwk(byte[] privateSeed, string kid)
    {
        var privateKey = new Ed25519PrivateKeyParameters(privateSeed, 0);
        var publicKey = privateKey.GeneratePublicKey().GetEncoded();
        return new() {
            ["kty"] = "OKP",
            ["crv"] = "Ed25519",
            ["use"] = "sig",
            ["alg"] = "EdDSA",
            ["kid"] = kid,
            ["x"] = Base64Url.Encode(publicKey)
        };
    }
}