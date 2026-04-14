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
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];
        using var aes = new System.Security.Cryptography.AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return (ciphertext, tag);
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] tag, byte[] key, byte[] nonce)
    {
        var plaintext = new byte[ciphertext.Length];
        using var aes = new System.Security.Cryptography.AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
