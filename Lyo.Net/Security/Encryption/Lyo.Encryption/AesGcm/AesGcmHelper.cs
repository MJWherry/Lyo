using Lyo.Exceptions;
#if !NET10_0_OR_GREATER
using System.Buffers;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
#endif

namespace Lyo.Encryption.AesGcm;

public static class AesGcmHelper
{
    public const int NonceSize = 12; // 96 bits

    public const int TagSize = 16; // 128 bits

    /// <summary>Throws if <paramref name="key" /> length is not a valid AES-GCM key size (16, 24, or 32 bytes).</summary>
    public static void ValidateKeyLength(ReadOnlySpan<byte> key, int expectedLengthBytes)
    {
        if (expectedLengthBytes is not (16 or 24 or 32))
            throw new ArgumentOutOfRangeException(nameof(expectedLengthBytes), expectedLengthBytes, "AES-GCM key length must be 16, 24, or 32 bytes.");

        ArgumentHelpers.ThrowIf(key.Length != expectedLengthBytes, $"AES-GCM key must be exactly {expectedLengthBytes} bytes; got {key.Length}.", nameof(key));
    }

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(byte[] plaintext, byte[] key, byte[] nonce, byte[]? associatedData = null)
        => Encrypt(plaintext.AsSpan(), key, nonce, associatedData);

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(ReadOnlySpan<byte> plaintext, byte[] key, byte[] nonce, byte[]? associatedData = null)
    {
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];
        Encrypt(plaintext, key, nonce, ciphertext, tag, associatedData);
        return (ciphertext, tag);
    }

    /// <summary>Encrypts into caller-provided <paramref name="ciphertext" /> and <paramref name="tag" /> buffers (must be sized to plaintext length and <see cref="TagSize" />).</summary>
    public static void Encrypt(ReadOnlySpan<byte> plaintext, byte[] key, ReadOnlySpan<byte> nonce, Span<byte> ciphertext, Span<byte> tag, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIf(
            ciphertext.Length != plaintext.Length, $"Ciphertext span length ({ciphertext.Length}) must equal plaintext length ({plaintext.Length}).", nameof(ciphertext));

        ArgumentHelpers.ThrowIf(tag.Length != TagSize, $"Tag span length ({tag.Length}) must be {TagSize}.", nameof(tag));
        ArgumentHelpers.ThrowIf(nonce.Length != NonceSize, $"Nonce length ({nonce.Length}) must be {NonceSize}.", nameof(nonce));
#if NET10_0_OR_GREATER
        using var aes = new System.Security.Cryptography.AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
#else
        var cipher = new GcmBlockCipher(new AesEngine());
        cipher.Init(true, new AeadParameters(new(key), 128, nonce.ToArray(), associatedData is { Length: > 0 } ? associatedData : null));
        var outLen = cipher.GetOutputSize(plaintext.Length);
        var outBuf = ArrayPool<byte>.Shared.Rent(outLen);
        try {
            var tlen = 0;
            if (plaintext.Length > 0) {
                var pb = ArrayPool<byte>.Shared.Rent(plaintext.Length);
                try {
                    plaintext.CopyTo(pb);
                    tlen = cipher.ProcessBytes(pb, 0, plaintext.Length, outBuf, 0);
                }
                finally {
                    ArrayPool<byte>.Shared.Return(pb, clearArray: true);
                }
            }

            tlen += cipher.DoFinal(outBuf, tlen);
            if (plaintext.Length > 0)
                outBuf.AsSpan(0, plaintext.Length).CopyTo(ciphertext);

            outBuf.AsSpan(plaintext.Length, TagSize).CopyTo(tag);
        }
        finally {
            ArrayPool<byte>.Shared.Return(outBuf, clearArray: true);
        }
#endif
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] tag, byte[] key, byte[] nonce, byte[]? associatedData = null)
    {
        var plaintext = new byte[ciphertext.Length];
        Decrypt(ciphertext, tag, key, nonce, plaintext, associatedData);
        return plaintext;
    }

    /// <summary>Decrypts into caller-provided <paramref name="plaintext" /> (must be sized to ciphertext length).</summary>
    public static void Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, byte[] key, ReadOnlySpan<byte> nonce, Span<byte> plaintext, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIf(
            plaintext.Length != ciphertext.Length, $"Plaintext span length ({plaintext.Length}) must equal ciphertext length ({ciphertext.Length}).", nameof(plaintext));

        ArgumentHelpers.ThrowIf(tag.Length != TagSize, $"Tag span length ({tag.Length}) must be {TagSize}.", nameof(tag));
        ArgumentHelpers.ThrowIf(nonce.Length != NonceSize, $"Nonce length ({nonce.Length}) must be {NonceSize}.", nameof(nonce));
#if NET10_0_OR_GREATER
        using var aes = new System.Security.Cryptography.AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
#else
        try {
            var combinedLen = ciphertext.Length + TagSize;
            var combined = ArrayPool<byte>.Shared.Rent(combinedLen);
            try {
                ciphertext.CopyTo(combined);
                tag.CopyTo(combined.AsSpan(ciphertext.Length));
                var cipher = new GcmBlockCipher(new AesEngine());
                cipher.Init(false, new AeadParameters(new(key), 128, nonce.ToArray(), associatedData is { Length: > 0 } ? associatedData : null));
                var outBuf = ArrayPool<byte>.Shared.Rent(cipher.GetOutputSize(combinedLen));
                try {
                    var len = cipher.ProcessBytes(combined, 0, combinedLen, outBuf, 0);
                    len += cipher.DoFinal(outBuf, len);
                    outBuf.AsSpan(0, len).CopyTo(plaintext);
                }
                finally {
                    ArrayPool<byte>.Shared.Return(outBuf, clearArray: true);
                }
            }
            finally {
                ArrayPool<byte>.Shared.Return(combined, clearArray: true);
            }
        }
        catch (InvalidCipherTextException ex) {
            throw new CryptographicException("AES-GCM authentication failed.", ex);
        }
#endif
    }
}