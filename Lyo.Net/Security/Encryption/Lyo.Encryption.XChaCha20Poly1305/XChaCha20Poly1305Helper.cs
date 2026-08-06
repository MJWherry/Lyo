using System.Buffers;
using System.Security.Cryptography;
using Lyo.Encryption.Security;
using Lyo.Exceptions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace Lyo.Encryption.XChaCha20Poly1305;

internal static class XChaCha20Poly1305Helper
{
    public const int NonceSize = 24;

    public const int TagSize = 16;

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(ReadOnlySpan<byte> plaintext, byte[] key, byte[] nonce24, byte[]? associatedData = null)
    {
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];
        Encrypt(plaintext, key, nonce24, ciphertext, tag, associatedData);
        return (ciphertext, tag);
    }

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(byte[] plaintext, byte[] key, byte[] nonce24, byte[]? associatedData = null)
        => Encrypt(plaintext.AsSpan(), key, nonce24, associatedData);

    /// <summary>Encrypts into caller-provided <paramref name="ciphertext" /> and <paramref name="tag" /> buffers.</summary>
    public static void Encrypt(
        ReadOnlySpan<byte> plaintext,
        byte[] key,
        ReadOnlySpan<byte> nonce24,
        Span<byte> ciphertext,
        Span<byte> tag,
        byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIf(ciphertext.Length != plaintext.Length, $"Ciphertext span length ({ciphertext.Length}) must equal plaintext length ({plaintext.Length}).", nameof(ciphertext));
        ArgumentHelpers.ThrowIf(tag.Length != TagSize, $"Tag span length ({tag.Length}) must be {TagSize}.", nameof(tag));
        ArgumentHelpers.ThrowIf(nonce24.Length != NonceSize, $"Nonce length ({nonce24.Length}) must be {NonceSize}.", nameof(nonce24));

        var subkey = new byte[32];
        try {
            HChaCha20.Block(key, nonce24[..16], subkey);
            Span<byte> nonce12 = stackalloc byte[12];
            nonce12.Clear();
            nonce24[16..].CopyTo(nonce12[4..]);
            var chacha = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
            chacha.Init(true, new AeadParameters(new(subkey), 128, nonce12.ToArray(), associatedData is { Length: > 0 } ? associatedData : null));
            var outLen = chacha.GetOutputSize(plaintext.Length);
            var outBuf = ArrayPool<byte>.Shared.Rent(outLen);
            try {
                var tlen = 0;
                if (plaintext.Length > 0) {
                    var pb = ArrayPool<byte>.Shared.Rent(plaintext.Length);
                    try {
                        plaintext.CopyTo(pb);
                        tlen = chacha.ProcessBytes(pb, 0, plaintext.Length, outBuf, 0);
                    }
                    finally {
                        ArrayPool<byte>.Shared.Return(pb, clearArray: true);
                    }
                }

                tlen += chacha.DoFinal(outBuf, tlen);
                if (plaintext.Length > 0)
                    outBuf.AsSpan(0, plaintext.Length).CopyTo(ciphertext);

                outBuf.AsSpan(plaintext.Length, TagSize).CopyTo(tag);
            }
            finally {
                ArrayPool<byte>.Shared.Return(outBuf, clearArray: true);
            }
        }
        finally {
            SecurityUtilities.Clear(subkey);
        }
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] tag, byte[] key, byte[] nonce24, byte[]? associatedData = null)
    {
        var plaintext = new byte[ciphertext.Length];
        Decrypt(ciphertext, tag, key, nonce24, plaintext, associatedData);
        return plaintext;
    }

    /// <summary>Decrypts into caller-provided <paramref name="plaintext" /> (must be sized to ciphertext length).</summary>
    public static void Decrypt(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> tag,
        byte[] key,
        ReadOnlySpan<byte> nonce24,
        Span<byte> plaintext,
        byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIf(plaintext.Length != ciphertext.Length, $"Plaintext span length ({plaintext.Length}) must equal ciphertext length ({ciphertext.Length}).", nameof(plaintext));
        ArgumentHelpers.ThrowIf(tag.Length != TagSize, $"Tag span length ({tag.Length}) must be {TagSize}.", nameof(tag));
        ArgumentHelpers.ThrowIf(nonce24.Length != NonceSize, $"Nonce length ({nonce24.Length}) must be {NonceSize}.", nameof(nonce24));

        var subkey = new byte[32];
        try {
            HChaCha20.Block(key, nonce24[..16], subkey);
            Span<byte> nonce12 = stackalloc byte[12];
            nonce12.Clear();
            nonce24[16..].CopyTo(nonce12[4..]);
            var combinedLen = ciphertext.Length + TagSize;
            var combined = ArrayPool<byte>.Shared.Rent(combinedLen);
            try {
                ciphertext.CopyTo(combined);
                tag.CopyTo(combined.AsSpan(ciphertext.Length));
                try {
                    var chacha = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
                    chacha.Init(false, new AeadParameters(new(subkey), 128, nonce12.ToArray(), associatedData is { Length: > 0 } ? associatedData : null));
                    var outBuf = ArrayPool<byte>.Shared.Rent(chacha.GetOutputSize(combinedLen));
                    try {
                        var len = chacha.ProcessBytes(combined, 0, combinedLen, outBuf, 0);
                        len += chacha.DoFinal(outBuf, len);
                        outBuf.AsSpan(0, len).CopyTo(plaintext);
                    }
                    finally {
                        ArrayPool<byte>.Shared.Return(outBuf, clearArray: true);
                    }
                }
                catch (InvalidCipherTextException ex) {
                    throw new CryptographicException("XChaCha20-Poly1305 authentication failed.", ex);
                }
            }
            finally {
                ArrayPool<byte>.Shared.Return(combined, clearArray: true);
            }
        }
        finally {
            SecurityUtilities.Clear(subkey);
        }
    }
}
