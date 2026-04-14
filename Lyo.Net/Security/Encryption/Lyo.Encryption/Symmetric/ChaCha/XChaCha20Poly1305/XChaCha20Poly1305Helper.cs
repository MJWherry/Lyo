using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Lyo.Encryption.Security;

namespace Lyo.Encryption.Symmetric.ChaCha.XChaCha20Poly1305;

internal static class XChaCha20Poly1305Helper
{
    public const int NonceSize = 24;

    public const int TagSize = 16;

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(ReadOnlySpan<byte> plaintext, byte[] key, byte[] nonce24)
    {
        var subkey = new byte[32];
        try {
            HChaCha20.Block(key, nonce24.AsSpan(0, 16), subkey);

            var nonce12 = new byte[12];
            nonce12[0] = 0;
            nonce12[1] = 0;
            nonce12[2] = 0;
            nonce12[3] = 0;
            Buffer.BlockCopy(nonce24, 16, nonce12, 4, 8);

            var chacha = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
            chacha.Init(true, new AeadParameters(new KeyParameter(subkey), 128, nonce12, null));

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
        }
        finally {
            SecurityUtilities.Clear(subkey);
        }
    }

    public static (byte[] Ciphertext, byte[] Tag) Encrypt(byte[] plaintext, byte[] key, byte[] nonce24) =>
        Encrypt(plaintext.AsSpan(), key, nonce24);

    public static byte[] Decrypt(byte[] ciphertext, byte[] tag, byte[] key, byte[] nonce24)
    {
        var subkey = new byte[32];
        try {
            HChaCha20.Block(key, nonce24.AsSpan(0, 16), subkey);

            var nonce12 = new byte[12];
            nonce12[0] = 0;
            nonce12[1] = 0;
            nonce12[2] = 0;
            nonce12[3] = 0;
            Buffer.BlockCopy(nonce24, 16, nonce12, 4, 8);

            var combined = new byte[ciphertext.Length + TagSize];
            Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, TagSize);

            try {
                var chacha = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
                chacha.Init(false, new AeadParameters(new KeyParameter(subkey), 128, nonce12, null));

                var outBuf = new byte[chacha.GetOutputSize(combined.Length)];
                var len = chacha.ProcessBytes(combined, 0, combined.Length, outBuf, 0);
                len += chacha.DoFinal(outBuf, len);
                var plaintext = new byte[len];
                Buffer.BlockCopy(outBuf, 0, plaintext, 0, len);
                return plaintext;
            }
            catch (InvalidCipherTextException ex) {
                throw new System.Security.Cryptography.CryptographicException("XChaCha20-Poly1305 authentication failed.", ex);
            }
        }
        finally {
            SecurityUtilities.Clear(subkey);
        }
    }
}
