using System.Security.Cryptography;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Security;
using Lyo.Exceptions;

namespace Lyo.Encryption.TwoKey;

/// <summary>
/// AES Key Wrap (RFC 3394). Used as the two-key stream DEK encoding when the KEK is a raw AES-sized key: the wrapped DEK is deterministic, integrity-checked, and exactly
/// <c>dekLength + 8</c> bytes — unlike a full AEAD envelope there is no nonce or header to carry, which complements the encrypted-DEK length cap. Implemented directly over AES-ECB so
/// it works on every target framework without extra dependencies.
/// </summary>
internal static class AesKeyWrap
{
    private const int BlockSize = 8;

    /// <summary>RFC 3394 default initial value; its presence after unwrapping proves integrity.</summary>
    private const ulong DefaultIv = 0xA6A6A6A6A6A6A6A6;

    /// <summary>True when <paramref name="kekLength" /> is a valid AES key size (16, 24 or 32 bytes).</summary>
    public static bool IsValidKekLength(int kekLength) => kekLength is 16 or 24 or 32;

    /// <summary>Wraps <paramref name="keyData" /> (multiple of 8 bytes, at least 16) under <paramref name="kek" />; output is <c>keyData.Length + 8</c> bytes.</summary>
    public static byte[] Wrap(byte[] kek, byte[] keyData)
    {
        ArgumentHelpers.ThrowIf(!IsValidKekLength(kek.Length), $"AES Key Wrap KEK must be 16, 24 or 32 bytes; got {kek.Length}.", nameof(kek));
        ArgumentHelpers.ThrowIf(
            keyData.Length < 2 * BlockSize || keyData.Length % BlockSize != 0, $"AES Key Wrap input must be a multiple of 8 bytes and at least 16 bytes; got {keyData.Length}.",
            nameof(keyData));

        var n = keyData.Length / BlockSize;
        var a = DefaultIv;
        var r = new byte[keyData.Length];
        keyData.CopyTo(r, 0);
        var block = new byte[16];
        try {
            using var aes = CreateEcb(kek);
            using var encryptor = aes.CreateEncryptor();
            for (var j = 0; j <= 5; j++) {
                for (var i = 1; i <= n; i++) {
                    WriteUInt64(block, 0, a);
                    Array.Copy(r, (i - 1) * BlockSize, block, BlockSize, BlockSize);
                    encryptor.TransformBlock(block, 0, 16, block, 0);
                    a = ReadUInt64(block, 0) ^ (ulong)(n * j + i);
                    Array.Copy(block, BlockSize, r, (i - 1) * BlockSize, BlockSize);
                }
            }

            var output = new byte[keyData.Length + BlockSize];
            WriteUInt64(output, 0, a);
            r.CopyTo(output, BlockSize);
            return output;
        }
        finally {
            // The block buffer starts each round holding 8 bytes of plaintext key material (r is ciphertext after the first pass).
            SecurityUtilities.Clear(block);
        }
    }

    /// <summary>Unwraps RFC 3394 <paramref name="wrapped" /> data under <paramref name="kek" />. Throws <see cref="DecryptionFailedException" /> when the integrity check fails.</summary>
    public static byte[] Unwrap(byte[] kek, byte[] wrapped)
    {
        ArgumentHelpers.ThrowIf(!IsValidKekLength(kek.Length), $"AES Key Wrap KEK must be 16, 24 or 32 bytes; got {kek.Length}.", nameof(kek));
        ArgumentHelpers.ThrowIf(
            wrapped.Length < 3 * BlockSize || wrapped.Length % BlockSize != 0,
            $"AES Key Wrap ciphertext must be a multiple of 8 bytes and at least 24 bytes; got {wrapped.Length}.", nameof(wrapped));

        var n = wrapped.Length / BlockSize - 1;
        var a = ReadUInt64(wrapped, 0);
        var r = new byte[n * BlockSize];
        Array.Copy(wrapped, BlockSize, r, 0, r.Length);
        var block = new byte[16];
        try {
            using var aes = CreateEcb(kek);
            using var decryptor = aes.CreateDecryptor();
            for (var j = 5; j >= 0; j--) {
                for (var i = n; i >= 1; i--) {
                    WriteUInt64(block, 0, a ^ (ulong)(n * j + i));
                    Array.Copy(r, (i - 1) * BlockSize, block, BlockSize, BlockSize);
                    decryptor.TransformBlock(block, 0, 16, block, 0);
                    a = ReadUInt64(block, 0);
                    Array.Copy(block, BlockSize, r, (i - 1) * BlockSize, BlockSize);
                }
            }

            if (a != DefaultIv)
                throw new DecryptionFailedException("AES Key Wrap integrity check failed. Possible causes: wrong KEK or corrupted wrapped key data.");

            return r;
        }
        finally {
            // The block buffer ends holding 8 bytes of the recovered plaintext key; r itself is returned to the caller, who is responsible for clearing it.
            SecurityUtilities.Clear(block);
        }
    }

    private static Aes CreateEcb(byte[] kek)
    {
        var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = kek;
        return aes;
    }

    private static ulong ReadUInt64(byte[] buffer, int offset)
        => ((ulong)buffer[offset] << 56) | ((ulong)buffer[offset + 1] << 48) | ((ulong)buffer[offset + 2] << 40) | ((ulong)buffer[offset + 3] << 32) |
            ((ulong)buffer[offset + 4] << 24) | ((ulong)buffer[offset + 5] << 16) | ((ulong)buffer[offset + 6] << 8) | buffer[offset + 7];

    private static void WriteUInt64(byte[] buffer, int offset, ulong value)
    {
        buffer[offset] = (byte)(value >> 56);
        buffer[offset + 1] = (byte)(value >> 48);
        buffer[offset + 2] = (byte)(value >> 40);
        buffer[offset + 3] = (byte)(value >> 32);
        buffer[offset + 4] = (byte)(value >> 24);
        buffer[offset + 5] = (byte)(value >> 16);
        buffer[offset + 6] = (byte)(value >> 8);
        buffer[offset + 7] = (byte)value;
    }
}