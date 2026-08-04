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
        ValidatePlaintextLength(plaintext.Length);
        var cipher = new CcmBlockCipher(new AesEngine());
        cipher.Init(true, new AeadParameters(new(key), 128, nonce, associatedData is { Length: > 0 } ? associatedData : null));
        byte[] packed;
        if (plaintext.Length == 0)
            packed = cipher.ProcessPacket([], 0, 0);
        else {
            var pb = new byte[plaintext.Length];
            plaintext.CopyTo(pb);
            packed = cipher.ProcessPacket(pb, 0, pb.Length);
        }

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        if (plaintext.Length > 0)
            Buffer.BlockCopy(packed, 0, ciphertext, 0, plaintext.Length);

        Buffer.BlockCopy(packed, plaintext.Length, tag, 0, TagSize);
        return (ciphertext, tag);
    }

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(byte[] plaintext, byte[] key, byte[] nonce, byte[]? associatedData = null)
        => Encrypt(plaintext.AsSpan(), key, nonce, associatedData);

    public static byte[] Decrypt(byte[] ciphertext, byte[] tag, byte[] key, byte[] nonce, byte[]? associatedData = null)
    {
        try {
            var combined = new byte[ciphertext.Length + TagSize];
            Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, TagSize);
            var cipher = new CcmBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new(key), 128, nonce, associatedData is { Length: > 0 } ? associatedData : null));
            return cipher.ProcessPacket(combined, 0, combined.Length);
        }
        catch (InvalidCipherTextException ex) {
            throw new CryptographicException("AES-CCM authentication failed.", ex);
        }
    }
}