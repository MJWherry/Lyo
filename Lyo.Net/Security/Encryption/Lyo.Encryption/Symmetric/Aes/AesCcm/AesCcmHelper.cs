using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Lyo.Encryption.Symmetric.Aes.AesCcm;

internal static class AesCcmHelper
{
    public const int NonceSize = 12;

    public const int TagSize = 16;

    public static void ValidateKeyLength(ReadOnlySpan<byte> key, int expectedLengthBytes)
    {
        if (expectedLengthBytes is not (16 or 24 or 32))
            throw new ArgumentOutOfRangeException(nameof(expectedLengthBytes), expectedLengthBytes, "AES-CCM key length must be 16, 24, or 32 bytes.");

        if (key.Length != expectedLengthBytes)
            throw new ArgumentException($"AES-CCM key must be exactly {expectedLengthBytes} bytes; got {key.Length}.", nameof(key));
    }

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(ReadOnlySpan<byte> plaintext, byte[] key, byte[] nonce)
    {
        var cipher = new CcmBlockCipher(new AesEngine());
        cipher.Init(true, new AeadParameters(new KeyParameter(key), 128, nonce, null));
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

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(byte[] plaintext, byte[] key, byte[] nonce) =>
        Encrypt(plaintext.AsSpan(), key, nonce);

    public static byte[] Decrypt(byte[] ciphertext, byte[] tag, byte[] key, byte[] nonce)
    {
        try {
            var combined = new byte[ciphertext.Length + TagSize];
            Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, TagSize);

            var cipher = new CcmBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new KeyParameter(key), 128, nonce, null));
            return cipher.ProcessPacket(combined, 0, combined.Length);
        }
        catch (InvalidCipherTextException ex) {
            throw new System.Security.Cryptography.CryptographicException("AES-CCM authentication failed.", ex);
        }
    }
}
