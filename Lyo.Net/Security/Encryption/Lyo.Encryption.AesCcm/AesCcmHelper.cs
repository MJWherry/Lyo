using System.Buffers;
using System.Security.Cryptography;
using Lyo.Exceptions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Lyo.Encryption.AesCcm;

internal static class AesCcmHelper
{
    public const int NonceSize = 12;

    public const int TagSize = 16;

    /// <summary>
    /// Maximum single-packet plaintext length for a 12-byte nonce (RFC 3610: <c>q = 15 - nonceLen = 3</c> → <c>2^(8q) - 1</c>).
    /// Larger payloads must use the streaming APIs (per-chunk CCM packets).
    /// </summary>
    public const int MaxPlaintextLength = (1 << 24) - 1; // 16_777_215

    public static void ValidateKeyLength(ReadOnlySpan<byte> key, int expectedLengthBytes)
    {
        if (expectedLengthBytes is not (16 or 24 or 32))
            throw new ArgumentOutOfRangeException(nameof(expectedLengthBytes), expectedLengthBytes, "AES-CCM key length must be 16, 24, or 32 bytes.");

        ArgumentHelpers.ThrowIf(key.Length != expectedLengthBytes, $"AES-CCM key must be exactly {expectedLengthBytes} bytes; got {key.Length}.", nameof(key));
    }

    public static void ValidatePlaintextLength(int plaintextLength, string paramName = "plaintext")
    {
        if (plaintextLength > MaxPlaintextLength) {
            throw new ArgumentOutOfRangeException(
                paramName,
                plaintextLength,
                $"AES-CCM with a {NonceSize}-byte nonce supports at most {MaxPlaintextLength} bytes per packet (~16 MiB). Use EncryptToStreamAsync / file streaming for larger payloads.");
        }
    }

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(ReadOnlySpan<byte> plaintext, byte[] key, byte[] nonce, byte[]? associatedData = null)
    {
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];
        Encrypt(plaintext, key, nonce, ciphertext, tag, associatedData);
        return (ciphertext, tag);
    }

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(byte[] plaintext, byte[] key, byte[] nonce, byte[]? associatedData = null)
        => Encrypt(plaintext.AsSpan(), key, nonce, associatedData);

    /// <summary>Encrypts into caller-provided <paramref name="ciphertext" /> and <paramref name="tag" /> (BC emits ct‖tag; results are split into the destinations).</summary>
    public static void Encrypt(
        ReadOnlySpan<byte> plaintext,
        byte[] key,
        ReadOnlySpan<byte> nonce,
        Span<byte> ciphertext,
        Span<byte> tag,
        byte[]? associatedData = null)
    {
        ValidatePlaintextLength(plaintext.Length);
        ArgumentHelpers.ThrowIf(ciphertext.Length != plaintext.Length, $"Ciphertext span length ({ciphertext.Length}) must equal plaintext length ({plaintext.Length}).", nameof(ciphertext));
        ArgumentHelpers.ThrowIf(tag.Length != TagSize, $"Tag span length ({tag.Length}) must be {TagSize}.", nameof(tag));
        ArgumentHelpers.ThrowIf(nonce.Length != NonceSize, $"Nonce length ({nonce.Length}) must be {NonceSize}.", nameof(nonce));

        var cipher = new CcmBlockCipher(new AesEngine());
        cipher.Init(true, new AeadParameters(new(key), 128, nonce.ToArray(), associatedData is { Length: > 0 } ? associatedData : null));
        byte[] packed;
        if (plaintext.Length == 0)
            packed = cipher.ProcessPacket([], 0, 0);
        else {
            var pb = ArrayPool<byte>.Shared.Rent(plaintext.Length);
            try {
                plaintext.CopyTo(pb);
                packed = cipher.ProcessPacket(pb, 0, plaintext.Length);
            }
            finally {
                ArrayPool<byte>.Shared.Return(pb, clearArray: true);
            }
        }

        if (plaintext.Length > 0)
            packed.AsSpan(0, plaintext.Length).CopyTo(ciphertext);

        packed.AsSpan(plaintext.Length, TagSize).CopyTo(tag);
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] tag, byte[] key, byte[] nonce, byte[]? associatedData = null)
    {
        var plaintext = new byte[ciphertext.Length];
        Decrypt(ciphertext, tag, key, nonce, plaintext, associatedData);
        return plaintext;
    }

    /// <summary>Decrypts into caller-provided <paramref name="plaintext" /> (must be sized to ciphertext length).</summary>
    public static void Decrypt(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> tag,
        byte[] key,
        ReadOnlySpan<byte> nonce,
        Span<byte> plaintext,
        byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIf(plaintext.Length != ciphertext.Length, $"Plaintext span length ({plaintext.Length}) must equal ciphertext length ({ciphertext.Length}).", nameof(plaintext));
        ArgumentHelpers.ThrowIf(tag.Length != TagSize, $"Tag span length ({tag.Length}) must be {TagSize}.", nameof(tag));
        ArgumentHelpers.ThrowIf(nonce.Length != NonceSize, $"Nonce length ({nonce.Length}) must be {NonceSize}.", nameof(nonce));
        try {
            var combinedLen = ciphertext.Length + TagSize;
            var combined = ArrayPool<byte>.Shared.Rent(combinedLen);
            try {
                ciphertext.CopyTo(combined);
                tag.CopyTo(combined.AsSpan(ciphertext.Length));
                var cipher = new CcmBlockCipher(new AesEngine());
                cipher.Init(false, new AeadParameters(new(key), 128, nonce.ToArray(), associatedData is { Length: > 0 } ? associatedData : null));
                var decrypted = cipher.ProcessPacket(combined, 0, combinedLen);
                decrypted.AsSpan(0, plaintext.Length).CopyTo(plaintext);
            }
            finally {
                ArrayPool<byte>.Shared.Return(combined, clearArray: true);
            }
        }
        catch (InvalidCipherTextException ex) {
            throw new CryptographicException("AES-CCM authentication failed.", ex);
        }
    }
}
