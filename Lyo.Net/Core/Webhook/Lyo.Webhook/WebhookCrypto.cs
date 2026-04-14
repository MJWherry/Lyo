using System.Security.Cryptography;
using System.Text;

namespace Lyo.Webhook;

/// <summary>Shared HMAC and comparison helpers for webhook provider libraries.</summary>
public static class WebhookCrypto
{
    /// <summary>Computes HMAC-SHA256 of <paramref name="data" /> using <paramref name="key" />.</summary>
    public static byte[] HmacSha256(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data)
    {
        using (var hmac = new HMACSHA256(key.ToArray()))
            return hmac.ComputeHash(data.ToArray());
    }

    /// <summary>Computes HMAC-SHA256 of <paramref name="data" /> using UTF-8 <paramref name="key" />.</summary>
    public static byte[] HmacSha256(string key, ReadOnlySpan<byte> data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        using (var hmac = new HMACSHA256(keyBytes))
            return hmac.ComputeHash(data.ToArray());
    }

    /// <summary>Computes HMAC-SHA1 of <paramref name="data" /> using UTF-8 <paramref name="key" />.</summary>
    public static byte[] HmacSha1(string key, ReadOnlySpan<byte> data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        using (var hmac = new HMACSHA1(keyBytes))
            return hmac.ComputeHash(data.ToArray());
    }

    /// <summary>Computes HMAC-SHA1 of <paramref name="data" /> using <paramref name="key" />.</summary>
    public static byte[] HmacSha1(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data)
    {
        using (var hmac = new HMACSHA1(key.ToArray()))
            return hmac.ComputeHash(data.ToArray());
    }

    /// <summary>Constant-time equality for MACs and digests.</summary>
    public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length)
            return false;

        var diff = 0;
        for (var i = 0; i < left.Length; i++)
            diff |= left[i] ^ right[i];

        return diff == 0;
    }

    /// <summary>Constant-time equality for ASCII signatures (e.g. hex or base64 strings of equal length).</summary>
    public static bool FixedTimeEquals(string? left, string? right)
    {
        if (left is null || right is null || left.Length != right.Length)
            return false;

        var diff = 0;
        for (var i = 0; i < left.Length; i++)
            diff |= left[i] ^ right[i];

        return diff == 0;
    }

    /// <summary>Parses a hex string into bytes. Returns null if length is odd or a character is invalid.</summary>
    public static byte[]? TryParseHex(string hex)
    {
        if (hex.Length % 2 != 0)
            return null;

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++) {
            var hi = ParseHexNibble(hex[i * 2]);
            var lo = ParseHexNibble(hex[i * 2 + 1]);
            if (hi < 0 || lo < 0)
                return null;

            bytes[i] = (byte)((hi << 4) | lo);
        }

        return bytes;
    }

    private static int ParseHexNibble(char c)
    {
        if (c >= '0' && c <= '9')
            return c - '0';

        if (c >= 'a' && c <= 'f')
            return c - 'a' + 10;

        if (c >= 'A' && c <= 'F')
            return c - 'A' + 10;

        return -1;
    }
}