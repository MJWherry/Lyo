using System.Security.Cryptography;
using System.Text.Json;
using Lyo.Authentication.Models.Format;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Core.Security;
using Lyo.Exceptions;
using Microsoft.AspNetCore.DataProtection;

namespace Lyo.Authentication.OpenIdConnect.Pkce;

/// <summary>Generates state tokens and seals/unseals <see cref="PkceState" /> using ASP.NET <see cref="IDataProtector" />.</summary>
public sealed class StateNonceProtector
{
    /// <summary>Data-protection purpose string used to derive the encryption key. Stable across deployments.</summary>
    public const string ProtectorPurpose = "Lyo.Authentication.OpenIdConnect.State.v1";

    private readonly IDataProtector _protector;

    /// <summary>Builds a new protector. Resolves a child protector under <see cref="ProtectorPurpose" />.</summary>
    public StateNonceProtector(IDataProtectionProvider provider)
    {
        ArgumentHelpers.ThrowIfNull(provider);
        _protector = provider.CreateProtector(ProtectorPurpose);
    }

    /// <summary>Generates a fresh OIDC <c>state</c> value (high-entropy, 32 random bytes base64url-encoded).</summary>
    public static string GenerateState() => Base64Url.Encode(CryptographicRandom.GetBytes(32));

    /// <summary>Generates a fresh OIDC <c>nonce</c> value (high-entropy, 32 random bytes base64url-encoded).</summary>
    public static string GenerateNonce() => Base64Url.Encode(CryptographicRandom.GetBytes(32));

    /// <summary>Seals <paramref name="state" /> into an opaque cookie value.</summary>
    public string Seal(PkceState state)
    {
        ArgumentHelpers.ThrowIfNull(state);
        var json = JsonSerializer.SerializeToUtf8Bytes(state);
        return Convert.ToBase64String(_protector.Protect(json));
    }

    /// <summary>Reverses <see cref="Seal" />. Returns <c>null</c> on tampering, expiry, or any other failure (caller treats this as <c>OidcStateInvalid</c>).</summary>
    public PkceState? Unseal(string? sealedValue)
    {
        if (sealedValue.IsNullOrWhitespace())
            return null;

        try {
            var encrypted = Convert.FromBase64String(sealedValue);
            var json = _protector.Unprotect(encrypted);
            return JsonSerializer.Deserialize<PkceState>(json);
        }
        catch (FormatException) {
            return null;
        }
        catch (CryptographicException) {
            return null;
        }
        catch (JsonException) {
            return null;
        }
    }
}