using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Lyo.Encryption.Streaming;

/// <summary>
/// Per-stream AEAD cipher bound to one key. Reused for every chunk of a streaming encrypt/decrypt so the key schedule is built once and no intermediate
/// arrays are allocated per chunk. Not thread-safe: one instance is driven sequentially by a single streaming loop and disposed when the stream ends. Created by
/// <see cref="EncryptionServiceBase.CreateStreamCryptor" /> on each single-key AEAD service.
/// </summary>
public interface IAeadStreamCryptor : IDisposable
{
    /// <summary>Nonce width in bytes (12 for AES-GCM / ChaCha20-Poly1305).</summary>
    int NonceSize { get; }

    /// <summary>Tag width in bytes (16 for AES-GCM / ChaCha20-Poly1305).</summary>
    int TagSize { get; }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> under <paramref name="nonce" /> and writes <c>ciphertext||tag</c> into <paramref name="ciphertextAndTag" />, which must be exactly
    /// <c>plaintext.Length + TagSize</c> bytes. Tag trails ciphertext so output is contiguous. <paramref name="associatedData" /> is authenticated but not encrypted (pass
    /// <see cref="ReadOnlySpan{T}.Empty" /> for none).
    /// </summary>
    void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag, ReadOnlySpan<byte> associatedData = default);

    /// <summary>
    /// Decrypts contiguous <c>ciphertext||tag</c> in <paramref name="ciphertextAndTag" /> under <paramref name="nonce" /> into <paramref name="plaintext" />, which must be
    /// exactly <c>ciphertextAndTag.Length - TagSize</c> bytes. <paramref name="associatedData" /> must equal the encrypt-time value. Throws
    /// <see cref="System.Security.Cryptography.CryptographicException" /> (or <c>AuthenticationTagMismatchException</c>) when the tag does not match.
    /// </summary>
    void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext, ReadOnlySpan<byte> associatedData = default);
}

/// <summary>
/// Encodes and decodes stream chunk frames into caller-owned buffers with no per-chunk heap allocation. Frame layout:
/// <c>[lengthAndFinalFlag:uint32 LE][ciphertext][tag:TagSize]</c> — nonce is derived (per-stream prefix + chunk counter) and never written; the high bit of the length
/// prefix marks the last chunk.
/// </summary>
internal static class AeadChunkCodec
{
    /// <summary>Fixed-width length prefix in front of each chunk body.</summary>
    public const int LengthPrefixSize = 4;

    /// <summary>Bit in the length prefix that marks the last chunk.</summary>
    public const uint FinalChunkFlag = 0x8000_0000u;

    /// <summary>Framing overhead per plaintext chunk: length prefix plus tag (nonce is derived, not stored).</summary>
    public static int Overhead(IAeadStreamCryptor cryptor) => LengthPrefixSize + cryptor.TagSize;

    /// <summary>
    /// Writes one frame (<c>[lengthAndFinalFlag][ciphertext][tag]</c>) for <paramref name="plaintext" /> into <paramref name="destination" /> and returns bytes
    /// written. The caller supplies the nonce (derived, never written) and <paramref name="associatedData" /> is authenticated with the chunk.
    /// </summary>
    public static int Encode(
        IAeadStreamCryptor cryptor,
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> associatedData,
        bool isFinal,
        Span<byte> destination)
    {
        var plaintextLength = plaintext.Length;
        var lengthAndFlag = (uint)plaintextLength | (isFinal ? FinalChunkFlag : 0u);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[..LengthPrefixSize], lengthAndFlag);
        cryptor.Encrypt(plaintext, nonce, destination.Slice(LengthPrefixSize, plaintextLength + cryptor.TagSize), associatedData);
        return LengthPrefixSize + plaintextLength + cryptor.TagSize;
    }

    /// <summary>
    /// Decrypts one chunk body (<c>ciphertext + tag</c>) of length <paramref name="bodyLength" /> in <paramref name="body" /> at offset 0 into <paramref name="plaintext" />
    /// using the caller-derived <paramref name="nonce" /> and <paramref name="associatedData" />; returns plaintext length. The length prefix must already have been read by the
    /// caller.
    /// </summary>
    public static int Decode(IAeadStreamCryptor cryptor, ReadOnlySpan<byte> body, int bodyLength, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, Span<byte> plaintext)
    {
        var ciphertextLength = bodyLength - cryptor.TagSize;
        cryptor.Decrypt(body[..bodyLength], nonce, plaintext[..ciphertextLength], associatedData);
        return ciphertextLength;
    }

    /// <summary>
    /// Guarantees <paramref name="buffer" /> is rented from <see cref="ArrayPool{T}" /> and at least <paramref name="size" /> bytes; grows (return + re-rent) if too small. When
    /// <paramref name="clearOnReturn" /> is true the discarded buffer is zeroed before it goes back to the pool (use for plaintext or key material).
    /// </summary>
    public static void EnsureCapacity([NotNull] ref byte[]? buffer, int size, bool clearOnReturn = false)
    {
        if (buffer != null && buffer.Length >= size)
            return;

        if (buffer != null)
            ArrayPool<byte>.Shared.Return(buffer, clearOnReturn);

        buffer = ArrayPool<byte>.Shared.Rent(size);
    }

    /// <summary>
    /// Reads up to <paramref name="count" /> bytes into <paramref name="buffer" /> at offset 0, looping until <paramref name="count" /> arrive or the stream ends.
    /// Returns bytes actually read (<paramref name="count" /> on success, 0 on a clean EOF, or a short count if the stream ended mid-read). Tolerates
    /// streams (for example pipes) that satisfy a read with fewer bytes than requested.
    /// </summary>
    public static async Task<int> ReadAtLeastAsync(Stream input, byte[] buffer, int count, CancellationToken ct)
    {
        var read = 0;
        while (read < count) {
            var n = await input.ReadAsync(buffer, read, count - read, ct).ConfigureAwait(false);
            if (n == 0)
                break;

            read += n;
        }

        return read;
    }
}