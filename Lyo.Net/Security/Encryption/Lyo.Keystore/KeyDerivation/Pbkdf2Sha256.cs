using System.Security.Cryptography;
using Lyo.Exceptions;

namespace Lyo.Keystore.KeyDerivation;

/// <summary>PBKDF2 with HMAC-SHA256 (RFC 2898).</summary>
internal static class Pbkdf2Sha256
{
    public static byte[] DeriveBytes(byte[] password, byte[] salt, int iterations, int dkLen)
    {
        ArgumentHelpers.ThrowIfLessThan(iterations, 1);
        ArgumentHelpers.ThrowIfLessThan(dkLen, 1);
        const int hLen = 32;
        var l = (dkLen + hLen - 1) / hLen;
        ArgumentHelpers.ThrowIfGreaterThan(l, int.MaxValue / hLen, nameof(dkLen));
        using var hmac = new HMACSHA256(password);
        var dk = new byte[dkLen];
        var offset = 0;
        for (var block = 1; block <= l; block++) {
            var t = F(hmac, salt, iterations, block);
            var copyLen = Math.Min(hLen, dkLen - offset);
            Buffer.BlockCopy(t, 0, dk, offset, copyLen);
            offset += copyLen;
        }

        return dk;
    }

    private static byte[] F(HMACSHA256 hmac, byte[] salt, int iterations, int block)
    {
        var saltLen = salt.Length;
        var buffer = new byte[saltLen + 4];
        Buffer.BlockCopy(salt, 0, buffer, 0, saltLen);
        buffer[saltLen] = (byte)(block >> 24);
        buffer[saltLen + 1] = (byte)(block >> 16);
        buffer[saltLen + 2] = (byte)(block >> 8);
        buffer[saltLen + 3] = (byte)block;
        var u = hmac.ComputeHash(buffer);
        var result = (byte[])u.Clone();
        for (var i = 1; i < iterations; i++) {
            u = hmac.ComputeHash(u);
            for (var j = 0; j < result.Length; j++)
                result[j] ^= u[j];
        }

        return result;
    }
}