using Lyo.Encryption.Security;
using Lyo.Encryption.Streaming;

namespace Lyo.Encryption.AesCcm;

/// <summary>
/// Per-stream AES-CCM cipher reused across every chunk of a streaming operation (see <see cref="IAeadStreamCryptor" />). Each chunk is encrypted independently with the
/// stream-supplied nonce via <see cref="AesCcmHelper" /> (BouncyCastle on every target framework, matching the single-shot wire format).
/// </summary>
internal sealed class AesCcmStreamCryptor(ReadOnlySpan<byte> key) : IAeadStreamCryptor
{
    private readonly byte[] _key = key.ToArray();

    public int NonceSize => AesCcmHelper.NonceSize;

    public int TagSize => AesCcmHelper.TagSize;

    public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag)
    {
        var (ciphertext, tag) = AesCcmHelper.Encrypt(plaintext, _key, nonce.ToArray());
        ciphertext.CopyTo(ciphertextAndTag[..plaintext.Length]);
        tag.CopyTo(ciphertextAndTag.Slice(plaintext.Length, AesCcmHelper.TagSize));
    }

    public void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext)
    {
        var ciphertext = ciphertextAndTag[..^AesCcmHelper.TagSize].ToArray();
        var tag = ciphertextAndTag[^AesCcmHelper.TagSize..].ToArray();
        var decrypted = AesCcmHelper.Decrypt(ciphertext, tag, _key, nonce.ToArray());
        decrypted.CopyTo(plaintext);
    }

    public void Dispose() => SecurityUtilities.Clear(_key);
}
