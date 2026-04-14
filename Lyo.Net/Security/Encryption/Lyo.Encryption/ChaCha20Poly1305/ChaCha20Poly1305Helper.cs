#if NET10_0_OR_GREATER
using System.Security.Cryptography;
#endif
#if !NET10_0_OR_GREATER
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
#endif

namespace Lyo.Encryption.ChaCha20Poly1305;

public static class ChaCha20Poly1305Helper
{
    public const int NonceSize = 12; // 96 bits

    public const int TagSize = 16; // 128 bits

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(byte[] plaintext, byte[] key, byte[] nonce) =>
        Encrypt(plaintext.AsSpan(), key, nonce);

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(ReadOnlySpan<byte> plaintext, byte[] key, byte[] nonce)
    {
#if NET10_0_OR_GREATER
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];
        using var chacha = new global::System.Security.Cryptography.ChaCha20Poly1305(key);
        chacha.Encrypt(nonce, plaintext, ciphertext, tag);
        return (ciphertext, tag);
#else
        var chacha = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
        chacha.Init(true, new AeadParameters(new KeyParameter(key), 128, nonce, null));

        var outBuf = new byte[chacha.GetOutputSize(plaintext.Length)];
        var tlen = 0;
        if (plaintext.Length > 0) {
            var pb = new byte[plaintext.Length];
            plaintext.CopyTo(pb);
            tlen = chacha.ProcessBytes(pb, 0, pb.Length, outBuf, 0);
        }

        tlen += chacha.DoFinal(outBuf, tlen);

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
        using var chacha = new global::System.Security.Cryptography.ChaCha20Poly1305(key);
        chacha.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
#else
        var combined = new byte[ciphertext.Length + TagSize];
        Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, TagSize);

        try {
            var chacha = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
            chacha.Init(false, new AeadParameters(new KeyParameter(key), 128, nonce, null));

            var outBuf = new byte[chacha.GetOutputSize(combined.Length)];
            var len = chacha.ProcessBytes(combined, 0, combined.Length, outBuf, 0);
            len += chacha.DoFinal(outBuf, len);
            var plaintext = new byte[len];
            Buffer.BlockCopy(outBuf, 0, plaintext, 0, len);
            return plaintext;
        }
        catch (InvalidCipherTextException ex) {
            throw new System.Security.Cryptography.CryptographicException("ChaCha20-Poly1305 authentication failed.", ex);
        }
#endif
    }
}
