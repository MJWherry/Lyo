using System;
using System.Security.Cryptography;
using System.Text.Json;
using Lyo.Authentication.Format;
using Lyo.Exceptions;
using Microsoft.AspNetCore.DataProtection;

namespace Lyo.Authentication.OpenIdConnect.Pkce;

/// <summary>Generates state tokens and seals/unseals <see cref="PkceState"/> using ASP.NET's <see cref="IDataProtector"/>.</summary>
public sealed class StateNonceProtector
{
    /// <summary>The data-protection purpose string used to derive the encryption key. Stable across deployments.</summary>
    public const string ProtectorPurpose = "Lyo.Authentication.OpenIdConnect.State.v1";

    private readonly IDataProtector _protector;

    /// <summary>Creates a new protector. Resolves a child protector under <see cref="ProtectorPurpose"/>.</summary>
    public StateNonceProtector(IDataProtectionProvider provider)
    {
        ArgumentHelpers.ThrowIfNull(provider);
        _protector = provider.CreateProtector(ProtectorPurpose);
    }

    /// <summary>Generates a fresh OIDC <c>state</c> value (high-entropy, 32 random bytes base64url-encoded).</summary>
    public static string GenerateState()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64Url.Encode(bytes);
    }

    /// <summary>Generates a fresh OIDC <c>nonce</c> value (high-entropy, 32 random bytes base64url-encoded).</summary>
    public static string GenerateNonce()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64Url.Encode(bytes);
    }

    /// <summary>Seals <paramref name="state"/> into an opaque cookie value.</summary>
    public string Seal(PkceState state)
    {
        ArgumentHelpers.ThrowIfNull(state);
        var json = JsonSerializer.SerializeToUtf8Bytes(state);
        return Convert.ToBase64String(_protector.Protect(json));
    }

    /// <summary>Reverses <see cref="Seal"/>. Returns <c>null</c> on tampering, expiry, or any other failure (caller treats this as <c>OidcStateInvalid</c>).</summary>
    public PkceState? Unseal(string? sealedValue)
    {
        if (string.IsNullOrWhiteSpace(sealedValue))
            return null;

        try {
            var encrypted = Convert.FromBase64String(sealedValue!);
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
