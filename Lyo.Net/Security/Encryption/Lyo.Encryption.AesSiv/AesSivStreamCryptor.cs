using Lyo.Encryption.Security;
using Lyo.Encryption.Streaming;

namespace Lyo.Encryption.AesSiv;

/// <summary>
/// Per-stream AES-SIV (RFC 5297) cipher for the streaming chunk loop (see <see cref="IAeadStreamCryptor" />). AES-SIV is deterministic and nonce-less, so the stream-supplied
/// per-chunk value (a 4-byte counter with no random prefix) is fed as associated data to bind each chunk's position while keeping streaming output deterministic for a given key,
/// plaintext, and chunk size. The 16-byte synthetic IV travels in the trailing tag slot expected by the codec (it is reassembled to the RFC 5297 <c>SIV || ciphertext</c> layout
/// internally).
/// </summary>
internal sealed class AesSivStreamCryptor(ReadOnlySpan<byte> key) : IAeadStreamCryptor
{
    private const int SivSize = 16;

    private readonly byte[] _key = key.ToArray();

    // Counter-only nonce: AeadStreamProcessor draws no random prefix (NonceSize - 4 == 0), so each chunk's
    // associated data is just its 4-byte little-endian index, preserving AES-SIV determinism.
    public int NonceSize => 4;

    public int TagSize => SivSize;

    public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag)
    {
        var total = new byte[SivSize + plaintext.Length];
        using (var siv = new Dorssel.Security.Cryptography.AesSiv(_key))
            siv.Encrypt(plaintext, total, nonce);

        // RFC 5297 produces [SIV(16)][ciphertext]; the codec expects [ciphertext][tag], so move the SIV to the tail.
        total.AsSpan(SivSize, plaintext.Length).CopyTo(ciphertextAndTag[..plaintext.Length]);
        total.AsSpan(0, SivSize).CopyTo(ciphertextAndTag.Slice(plaintext.Length, SivSize));
    }

    public void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext)
    {
        var ciphertextLength = ciphertextAndTag.Length - SivSize;
        var combined = new byte[ciphertextAndTag.Length];
        // Rebuild RFC 5297 [SIV(16)][ciphertext] from the codec's [ciphertext][tag] framing.
        ciphertextAndTag[ciphertextLength..].CopyTo(combined.AsSpan(0, SivSize));
        ciphertextAndTag[..ciphertextLength].CopyTo(combined.AsSpan(SivSize));
        using var siv = new Dorssel.Security.Cryptography.AesSiv(_key);
        siv.Decrypt(combined, plaintext, nonce);
    }

    public void Dispose() => SecurityUtilities.Clear(_key);
}