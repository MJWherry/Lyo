using System.Text;
using Lyo.Authentication.Models.Format;
using Lyo.Common.Extensions;
using Lyo.Common.Security;
using Lyo.Exceptions;
using Lyo.Hashing;

namespace Lyo.Authentication.Format;

/// <summary>Encodes, decodes, and hashes Format-B Lyo opaque tokens.</summary>
/// <remarks>
/// Wire format: <c>lyo_&lt;kind&gt;_&lt;ring&gt;_&lt;id&gt;_&lt;secret&gt;</c> where
/// <list type="bullet">
/// <item><c>kind</c> is a lowercase ASCII string (typically one of <see cref="ApiTokenKind" />)</item>
/// <item><c>ring</c> is a lowercase ASCII string (typically one of <see cref="ApiTokenRing" />)</item>
/// <item><c>id</c> is 11 lowercase Crockford base32 chars over 64 random bits</item> <item><c>secret</c> is 43 chars of base64url (32 random bytes, ~256 bits of entropy)</item>
/// </list>
/// Verification is always: parse → look up by <c>id</c> → constant-time-compare <see cref="ComputeSecretHash(string)" /> against the stored hash.
/// </remarks>
public static class ApiTokenCodec
{
    private const string Prefix = "lyo";
    private const char Separator = '_';

    /// <summary>The byte length of the secret material (32 = 256 bits of entropy).</summary>
    public const int SecretByteLength = 32;

    /// <summary>The expected character length of the encoded secret segment.</summary>
    public const int EncodedSecretLength = 43;

    /// <summary>
    /// Builds a new, freshly-randomized Format-B token for the given <paramref name="kind" /> and <paramref name="ring" />. Returns both the wire-form plaintext and its SHA-256
    /// secret hash for persistence. The caller is responsible for ensuring the id is unique (collision probability is ~2^-27.5 after 1 billion issuances, so retry-on-conflict at the
    /// store level is the right pattern).
    /// </summary>
    public static (string Plaintext, string Id, byte[] SecretHash) Mint(string kind, string ring)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(kind);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ring);
        ValidateSegment(kind, nameof(kind));
        ValidateSegment(ring, nameof(ring));
        var idBytes = CryptographicRandom.GetBytes(8);
        var id = Base32Crockford.EncodeRandom11(idBytes);
        var secretBytes = CryptographicRandom.GetBytes(SecretByteLength);
        var secret = Base64Url.Encode(secretBytes);
        var plaintext = Encode(kind, ring, id, secret);
        var hash = ComputeSecretHash(secret);
        Array.Clear(secretBytes, 0, secretBytes.Length);
        return (plaintext, id, hash);
    }

    /// <summary>Builds the wire-form string from already-decided segments. Does not allocate randomness — use <see cref="Mint" /> for new tokens.</summary>
    public static string Encode(string kind, string ring, string id, string secret)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(kind);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ring);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(secret);
        ValidateSegment(kind, nameof(kind));
        ValidateSegment(ring, nameof(ring));
        return $"{Prefix}{Separator}{kind}{Separator}{ring}{Separator}{id}{Separator}{secret}";
    }

    /// <summary>
    /// Attempts to parse a token off the wire. Returns <c>false</c> with <paramref name="token" /> = <c>null</c> for any malformed input (no exceptions on the validation hot
    /// path).
    /// </summary>
    /// <remarks>
    /// The secret segment is base64url and CAN contain underscores, so we cannot simply split on '_'. We instead pin the first four '_' positions (one per fixed segment
    /// boundary) and treat the remainder as the secret.
    /// </remarks>
    public static bool TryParse(string? input, out ApiToken? token)
    {
        token = null;
        if (input.IsNullOrWhitespace())
            return false;

        var s = input;
        var sep1 = s.IndexOf(Separator);
        if (sep1 != 3)
            return false;

        if (!string.Equals(s.Substring(0, 3), Prefix, StringComparison.Ordinal))
            return false;

        var sep2 = s.IndexOf(Separator, sep1 + 1);
        if (sep2 < 0)
            return false;

        var sep3 = s.IndexOf(Separator, sep2 + 1);
        if (sep3 < 0)
            return false;

        var sep4 = s.IndexOf(Separator, sep3 + 1);
        if (sep4 < 0)
            return false;

        var kind = s.Substring(sep1 + 1, sep2 - sep1 - 1);
        var ring = s.Substring(sep2 + 1, sep3 - sep2 - 1);
        var id = s.Substring(sep3 + 1, sep4 - sep3 - 1);
        var secret = s.Substring(sep4 + 1);
        if (kind.Length == 0 || !IsLowercaseAlpha(kind))
            return false;

        if (ring.Length == 0 || !IsLowercaseAlpha(ring))
            return false;

        if (!Base32Crockford.IsValidId(id))
            return false;

        if (secret.Length != EncodedSecretLength || !Base64Url.IsValid(secret))
            return false;

        token = new(input, id, kind, ring, secret);
        return true;
    }

    /// <summary>Computes the canonical SHA-256 hash of a secret segment for storage and constant-time comparison.</summary>
    public static byte[] ComputeSecretHash(string secret)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(secret);
        return Hasher.ComputeSha256(Encoding.UTF8.GetBytes(secret));
    }

    private static bool IsLowercaseAlpha(string s)
    {
        foreach (var c in s) {
            if (c is < 'a' or > 'z')
                return false;
        }

        return true;
    }

    private static void ValidateSegment(string value, string paramName)
    {
        if (!IsLowercaseAlpha(value))
            throw new ArgumentException($"Segment '{paramName}' must be lowercase ASCII letters only.", paramName);
    }
}