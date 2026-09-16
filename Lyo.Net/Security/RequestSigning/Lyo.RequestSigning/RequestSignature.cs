using System.Globalization;
using System.Text;
using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.Hashing;

namespace Lyo.RequestSigning;

/// <summary>Canonical bytes and header text for scheme <c>v1</c>.</summary>
public static class RequestSignature
{
    /// <summary>Canonicalization scheme written in the header. This is not a secret version.</summary>
    public const string Scheme = "v1";

    /// <summary>Builds <c>t=&lt;unix&gt;,v1=&lt;hex&gt;</c>.</summary>
    public static string FormatHeader(long unixSeconds, string macHex)
        => string.Concat("t=", unixSeconds.ToString(CultureInfo.InvariantCulture), ",v1=", macHex);

    /// <summary>Reads <c>t</c> and <c>v1</c>. Returns false when the shape is wrong.</summary>
    public static bool TryParse(string header, out long unixSeconds, out string macHex)
    {
        unixSeconds = 0;
        macHex = string.Empty;
        var comma = header.IndexOf(',');
        if (comma <= 0 || header.IndexOf(',', comma + 1) >= 0)
            return false;
        var timePart = header.Substring(0, comma);
        var macPart = header.Substring(comma + 1);
        if (!timePart.StartsWith("t=", StringComparison.Ordinal) || !macPart.StartsWith("v1=", StringComparison.Ordinal))
            return false;
        if (!long.TryParse(timePart.Substring(2), NumberStyles.None, CultureInfo.InvariantCulture, out unixSeconds) || unixSeconds < 0)
            return false;
        macHex = macPart.Substring(3);
        return macHex.Length == 64;
    }

    /// <summary>UTF-8 canonical bytes: scheme, unix seconds, method, scheme, host, path, and body hash, each on its own line.</summary>
    public static byte[] CanonicalBytes(RequestSignatureInput input, long unixSeconds, IHashingService hashing)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(hashing);
        var bodyHash = hashing.ToHex(hashing.Hash(ContentDigestAlgorithm.Sha256, input.Body.Span), TextLetterCase.Lower);
        var text = string.Join(
            "\n",
            Scheme,
            unixSeconds.ToString(CultureInfo.InvariantCulture),
            input.Method.ToUpperInvariant(),
            input.Scheme.ToLowerInvariant(),
            input.Host,
            input.PathAndQuery,
            bodyHash);
        return Encoding.UTF8.GetBytes(text);
    }

    /// <summary>HMAC-SHA256 of the canonical bytes using the UTF-8 secret.</summary>
    public static byte[] Mac(string secret, RequestSignatureInput input, long unixSeconds, IHashingService hashing)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        return hashing.HmacSha256(key, CanonicalBytes(input, unixSeconds, hashing));
    }
}
