using System.Security.Cryptography;
using Lyo.Exceptions;

namespace Lyo.Keystore.KeyDerivation;

/// <summary>RFC 5869 HKDF using HMAC-SHA256 (for targets without <see cref="HKDF" />).</summary>
internal static class HkdfRfc5869
{
    private const int HashLen = 32;

    public static byte[] Extract(byte[] salt, byte[] ikm)
    {
        var key = salt.Length == 0 ? new byte[HashLen] : salt;
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(ikm);
    }

    public static byte[] Expand(byte[] prk, int length, byte[] info)
    {
        ArgumentHelpers.ThrowIfNotInRange(length, 0, HashLen * 255);
        var n = (length + HashLen - 1) / HashLen;
        var okm = new byte[length];
        var offset = 0;
        var t = Array.Empty<byte>();
        for (var i = 1; i <= n; i++) {
            using var hmac = new HMACSHA256(prk);
            var dataLen = t.Length + info.Length + 1;
            var data = new byte[dataLen];
            Buffer.BlockCopy(t, 0, data, 0, t.Length);
            Buffer.BlockCopy(info, 0, data, t.Length, info.Length);
            data[^1] = (byte)i;
            t = hmac.ComputeHash(data);
            var copy = Math.Min(HashLen, length - offset);
            Buffer.BlockCopy(t, 0, okm, offset, copy);
            offset += copy;
        }

        return okm;
    }
}