namespace Lyo.Encryption.ChaCha20Poly1305;

public static class ChaCha20Poly1305Helper
{
    public const int NonceSize = 12; // 96 bits

    public const int TagSize = 16; // 128 bits

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(byte[] plaintext, byte[] key, byte[] nonce) =>
        Encrypt(plaintext.AsSpan(), key, nonce);

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(ReadOnlySpan<byte> plaintext, byte[] key, byte[] nonce)
    {
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];
        using var chacha = new System.Security.Cryptography.ChaCha20Poly1305(key);
        chacha.Encrypt(nonce, plaintext, ciphertext, tag);
        return (ciphertext, tag);
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] tag, byte[] key, byte[] nonce)
    {
        var plaintext = new byte[ciphertext.Length];
        using var chacha = new System.Security.Cryptography.ChaCha20Poly1305(key);
        chacha.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
