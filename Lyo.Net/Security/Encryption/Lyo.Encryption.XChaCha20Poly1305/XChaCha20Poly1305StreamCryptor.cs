using Lyo.Encryption.Security;
using Lyo.Encryption.Streaming;

namespace Lyo.Encryption.XChaCha20Poly1305;

/// <summary>
/// Per-stream XChaCha20-Poly1305 cipher reused across every chunk of a streaming operation (see <see cref="IAeadStreamCryptor" />). Each chunk is encrypted independently
/// with the stream-supplied 24-byte nonce via <see cref="XChaCha20Poly1305Helper" /> (HChaCha20 subkey derivation + IETF ChaCha20-Poly1305).
/// </summary>
internal sealed class XChaCha20Poly1305StreamCryptor(ReadOnlySpan<byte> key) : IAeadStreamCryptor
{
    private readonly byte[] _key = key.ToArray();

    public int NonceSize => XChaCha20Poly1305Helper.NonceSize;

    public int TagSize => XChaCha20Poly1305Helper.TagSize;

    public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag)
    {
        var (ciphertext, tag) = XChaCha20Poly1305Helper.Encrypt(plaintext, _key, nonce.ToArray());
        ciphertext.CopyTo(ciphertextAndTag[..plaintext.Length]);
        tag.CopyTo(ciphertextAndTag.Slice(plaintext.Length, XChaCha20Poly1305Helper.TagSize));
    }

    public void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext)
    {
        var ciphertext = ciphertextAndTag[..^XChaCha20Poly1305Helper.TagSize].ToArray();
        var tag = ciphertextAndTag[^XChaCha20Poly1305Helper.TagSize..].ToArray();
        var decrypted = XChaCha20Poly1305Helper.Decrypt(ciphertext, tag, _key, nonce.ToArray());
        decrypted.CopyTo(plaintext);
    }

    public void Dispose() => SecurityUtilities.Clear(_key);
}