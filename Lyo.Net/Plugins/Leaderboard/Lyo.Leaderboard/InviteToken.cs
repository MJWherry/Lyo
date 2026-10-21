using System.Security.Cryptography;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;

namespace Lyo.Leaderboard;

/// <summary>Creates and hashes invite tokens. Only the hash is persisted.</summary>
public static class InviteToken
{
    /// <summary>Generates a URL-safe plaintext token.</summary>
    public static string Create() => NanoId.Create(32);

    /// <summary>SHA-256 hex digest of <paramref name="token" />.</summary>
    public static string Hash(string token)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(token);
        var text = ByteConvert.GetBytes(token.Trim());
#if NETSTANDARD2_0
        using var sha = SHA256.Create();
        return ByteConvert.ToHex(sha.ComputeHash(text), TextLetterCase.Lower);
#else
        return ByteConvert.ToHex(SHA256.HashData(text), TextLetterCase.Lower);
#endif
    }
}