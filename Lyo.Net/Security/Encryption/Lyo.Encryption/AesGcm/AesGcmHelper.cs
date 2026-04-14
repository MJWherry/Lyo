#if NET10_0_OR_GREATER
using System.Security.Cryptography;
#endif
#if !NET10_0_OR_GREATER
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
#endif

namespace Lyo.Encryption.AesGcm;

public static class AesGcmHelper
{
    public const int NonceSize = 12; // 96 bits

    public const int TagSize = 16; // 128 bits

    /// <summary>Throws if <paramref name="key"/> length is not a valid AES-GCM key size (16, 24, or 32 bytes).</summary>
    public static void ValidateKeyLength(ReadOnlySpan<byte> key, int expectedLengthBytes)
    {
        if (expectedLengthBytes is not (16 or 24 or 32))
            throw new ArgumentOutOfRangeException(nameof(expectedLengthBytes), expectedLengthBytes, "AES-GCM key length must be 16, 24, or 32 bytes.");

        if (key.Length != expectedLengthBytes)
            throw new ArgumentException($"AES-GCM key must be exactly {expectedLengthBytes} bytes; got {key.Length}.", nameof(key));
    }

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(byte[] plaintext, byte[] key, byte[] nonce) =>
        Encrypt(plaintext.AsSpan(), key, nonce);

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(ReadOnlySpan<byte> plaintext, byte[] key, byte[] nonce)
    {
#if NET10_0_OR_GREATER
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];
        using var aes = new global::System.Security.Cryptography.AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return (ciphertext, tag);
#else
        var cipher = new GcmBlockCipher(new AesEngine());
        cipher.Init(true, new AeadParameters(new KeyParameter(key), 128, nonce, null));

        var outBuf = new byte[cipher.GetOutputSize(plaintext.Length)];
        var tlen = 0;
        if (plaintext.Length > 0) {
            var pb = new byte[plaintext.Length];
            plaintext.CopyTo(pb);
            tlen = cipher.ProcessBytes(pb, 0, pb.Length, outBuf, 0);
        }

        tlen += cipher.DoFinal(outBuf, tlen);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        if (plaintext.Length > 0)
            Buffer.BlockCopy(outBuf, 0, ciphertext, 0, plaintext.Length);

        Buffer.BlockCopy(outBuf, plaintext.Length, tag, 0, TagSize);
        return (ciphertext, tag);
#endif
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] tag, byte[] key, byte[] nonce)
    {
#if NET10_0_OR_GREATER
        var plaintext = new byte[ciphertext.Length];
        using var aes = new global::System.Security.Cryptography.AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
#else
        try {
            var combined = new byte[ciphertext.Length + TagSize];
            Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, TagSize);

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new KeyParameter(key), 128, nonce, null));

            var outBuf = new byte[cipher.GetOutputSize(combined.Length)];
            var len = cipher.ProcessBytes(combined, 0, combined.Length, outBuf, 0);
            len += cipher.DoFinal(outBuf, len);
            var plaintext = new byte[len];
            Buffer.BlockCopy(outBuf, 0, plaintext, 0, len);
            return plaintext;
        }
        catch (InvalidCipherTextException ex) {
            throw new System.Security.Cryptography.CryptographicException("AES-GCM authentication failed.", ex);
        }
#endif
    }
}
