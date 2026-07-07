using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Lyo.Encryption.Streaming;

/// <summary>
/// A per-stream AEAD cipher bound to a single key. Reused across every chunk of one streaming encrypt/decrypt operation so the key schedule is built once and no intermediate
/// arrays are allocated per chunk. Instances are NOT thread-safe: a single instance is driven sequentially by one streaming loop and disposed when the stream completes. Created by
/// <see cref="EncryptionServiceBase.CreateStreamCryptor" /> on each single-key AEAD service.
/// </summary>
public interface IAeadStreamCryptor : IDisposable
{
    /// <summary>Nonce size in bytes (12 for AES-GCM / ChaCha20-Poly1305).</summary>
    int NonceSize { get; }

    /// <summary>Authentication tag size in bytes (16 for AES-GCM / ChaCha20-Poly1305).</summary>
    int TagSize { get; }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> with <paramref name="nonce" /> and writes <c>ciphertext||tag</c> into <paramref name="ciphertextAndTag" />, which must be exactly
    /// <c>plaintext.Length + TagSize</c> bytes. The tag trails the ciphertext so the output is contiguous. <paramref name="associatedData" /> is authenticated but not encrypted
    /// (pass <see cref="ReadOnlySpan{T}.Empty" /> for none).
    /// </summary>
    void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag, ReadOnlySpan<byte> associatedData = default);

    /// <summary>
    /// Decrypts the contiguous <c>ciphertext||tag</c> in <paramref name="ciphertextAndTag" /> with <paramref name="nonce" /> into <paramref name="plaintext" />, which must be
    /// exactly <c>ciphertextAndTag.Length - TagSize</c> bytes. <paramref name="associatedData" /> must match the value supplied at encryption time. Throws
    /// <see cref="System.Security.Cryptography.CryptographicException" /> (or <c>AuthenticationTagMismatchException</c>) on tag mismatch.
    /// </summary>
    void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext, ReadOnlySpan<byte> associatedData = default);
}

/// <summary>
/// Encodes/decodes streaming chunk frames into caller-owned buffers with no per-chunk heap allocation. Frame layout:
/// <c>[lengthAndFinalFlag:uint32 LE][ciphertext][tag:TagSize]</c> — the nonce is derived (per-stream prefix + chunk counter) and never written to the wire; the top bit of the
/// length prefix marks the final chunk.
/// </summary>
internal static class AeadChunkCodec
{
    /// <summary>Fixed-size length prefix preceding each chunk body.</summary>
    public const int LengthPrefixSize = 4;

    /// <summary>Bit marking the final chunk in the length prefix.</summary>
    public const uint FinalChunkFlag = 0x8000_0000u;

    /// <summary>Bytes of framing overhead added to each plaintext chunk: length prefix + tag (the nonce is derived, not stored).</summary>
    public static int Overhead(IAeadStreamCryptor cryptor) => LengthPrefixSize + cryptor.TagSize;

    /// <summary>
    /// Writes one chunk (<c>[lengthAndFinalFlag][ciphertext][tag]</c>) for <paramref name="plaintext" /> into <paramref name="destination" /> and returns the total number of
    /// bytes written. The nonce is supplied by the caller (derived, never written to the wire) and <paramref name="associatedData" /> is authenticated with the chunk.
    /// </summary>
    public static int Encode(IAeadStreamCryptor cryptor, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, bool isFinal, Span<byte> destination)
    {
        var plaintextLength = plaintext.Length;
        var lengthAndFlag = (uint)plaintextLength | (isFinal ? FinalChunkFlag : 0u);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[..LengthPrefixSize], lengthAndFlag);
        cryptor.Encrypt(plaintext, nonce, destination.Slice(LengthPrefixSize, plaintextLength + cryptor.TagSize), associatedData);
        return LengthPrefixSize + plaintextLength + cryptor.TagSize;
    }

    /// <summary>
    /// Decrypts one chunk body (<c>ciphertext + tag</c>) of length <paramref name="bodyLength" /> held in <paramref name="body" /> at offset 0 into
    /// <paramref name="plaintext" /> using the caller-derived <paramref name="nonce" /> and <paramref name="associatedData" />; returns the plaintext length. The length prefix must
    /// already have been consumed by the caller.
    /// </summary>
    public static int Decode(IAeadStreamCryptor cryptor, ReadOnlySpan<byte> body, int bodyLength, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, Span<byte> plaintext)
    {
        var ciphertextLength = bodyLength - cryptor.TagSize;
        cryptor.Decrypt(body[..bodyLength], nonce, plaintext[..ciphertextLength], associatedData);
        return ciphertextLength;
    }

    /// <summary>
    /// Ensures <paramref name="buffer" /> is rented from <see cref="ArrayPool{T}" /> and at least <paramref name="size" /> bytes; grows (return + re-rent) if too small. When
    /// <paramref name="clearOnReturn" /> is true the outgrown buffer is zeroed before returning to the shared pool (use for buffers holding plaintext or key material).
    /// </summary>
    public static void EnsureCapacity([NotNull]ref byte[]? buffer, int size, bool clearOnReturn = false)
    {
        if (buffer != null && buffer.Length >= size)
            return;

        if (buffer != null)
            ArrayPool<byte>.Shared.Return(buffer, clearOnReturn);

        buffer = ArrayPool<byte>.Shared.Rent(size);
    }

    /// <summary>
    /// Reads up to <paramref name="count" /> bytes into <paramref name="buffer" /> at offset 0, looping until <paramref name="count" /> bytes are read or the stream ends.
    /// Returns the number of bytes actually read (equal to <paramref name="count" /> on success, 0 on a clean end of stream, or a partial count if the stream ends mid-read). Robust
    /// against streams (e.g. pipes) that satisfy a read with fewer bytes than requested.
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
