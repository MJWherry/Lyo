using System.Security.Cryptography;
using Lyo.Common.Core.Security;

namespace Lyo.FileMetadataStore.DownloadAccess;

/// <summary>
/// Issues, encodes, and hashes download-link tokens. Every provider must share this encoding, otherwise a token issued against one metadata store cannot be redeemed against
/// another backed by the same rows.
/// </summary>
/// <remarks>
/// <para>Tokens are unpadded base64url so they survive a URL path or query string untouched. Only the SHA-256 hash is persisted.</para>
/// </remarks>
public static class FileDownloadAccessToken
{
    /// <summary>Token entropy in bytes. 32 bytes keeps brute force infeasible while the encoded token stays short enough for a URL.</summary>
    public const int TokenBytes = 32;

    /// <summary>Prefix for the per-token lock key, isolating download-link locks inside a shared lock store.</summary>
    public const string LockKeyPrefix = "appstore:download-link:";

    /// <summary>Issues a fresh token and returns both the caller-facing string and the hash to persist.</summary>
    public static (string Token, byte[] TokenHash) Create()
    {
        var raw = CryptographicRandom.GetBytes(TokenBytes);
        return (Encode(raw), Hash(raw));
    }

    /// <summary>Hashes a caller-supplied token for lookup, or returns <see langword="null" /> when it is empty or not valid base64url.</summary>
    /// <param name="token">Token as presented by the caller.</param>
    public static byte[]? TryHash(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try {
            return Hash(Decode(token!));
        }
        catch (FormatException) {
            return null;
        }
    }

    /// <summary>Lock key that serializes concurrent redemptions of one token.</summary>
    /// <param name="tokenHash">Hash returned by <see cref="TryHash" /> or <see cref="Create" />.</param>
    public static string LockKey(byte[] tokenHash) => LockKeyPrefix + ToLowerHex(tokenHash);

    /// <summary>Renders bytes as unpadded base64url.</summary>
    /// <param name="input">Bytes to encode.</param>
    public static string Encode(byte[] input) => Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Parses unpadded base64url back to bytes.</summary>
    /// <param name="input">Encoded token.</param>
    /// <exception cref="FormatException">The input is not valid base64url.</exception>
    public static byte[] Decode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch {
            2 => padded + "==",
            3 => padded + "=",
            var _ => padded
        };

        return Convert.FromBase64String(padded);
    }

    private static byte[] Hash(byte[] input)
    {
#if NET6_0_OR_GREATER
        return SHA256.HashData(input);
#else
        using var sha = SHA256.Create();
        return sha.ComputeHash(input);
#endif
    }

    private static string ToLowerHex(byte[] input)
    {
#if NET6_0_OR_GREATER
        return Convert.ToHexString(input).ToLowerInvariant();
#else
        var chars = new char[input.Length * 2];
        for (var i = 0; i < input.Length; i++) {
            chars[i * 2] = "0123456789abcdef"[input[i] >> 4];
            chars[(i * 2) + 1] = "0123456789abcdef"[input[i] & 0xF];
        }

        return new(chars);
#endif
    }
}
