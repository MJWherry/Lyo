using System.Buffers;
using System.Buffers.Binary;

namespace Lyo.Encryption.Streaming;

/// <summary>
/// A per-stream AEAD cipher bound to a single key. Reused across every chunk of one streaming encrypt/decrypt operation so the key schedule is built once and no intermediate
/// arrays are allocated per chunk. Instances are NOT thread-safe: a single instance is driven sequentially by one streaming loop and disposed when the stream completes. Created
/// by <see cref="EncryptionServiceBase.CreateStreamCryptor" /> on each single-key AEAD service.
/// </summary>
public interface IAeadStreamCryptor : IDisposable
{
    /// <summary>Nonce size in bytes (12 for AES-GCM / ChaCha20-Poly1305).</summary>
    int NonceSize { get; }

    /// <summary>Authentication tag size in bytes (16 for AES-GCM / ChaCha20-Poly1305).</summary>
    int TagSize { get; }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> with <paramref name="nonce" /> and writes <c>ciphertext||tag</c> into <paramref name="ciphertextAndTag" />, which must be exactly
    /// <c>plaintext.Length + TagSize</c> bytes. The tag trails the ciphertext so the output is contiguous.
    /// </summary>
    void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> ciphertextAndTag);

    /// <summary>
    /// Decrypts the contiguous <c>ciphertext||tag</c> in <paramref name="ciphertextAndTag" /> with <paramref name="nonce" /> into <paramref name="plaintext" />, which must be
    /// exactly <c>ciphertextAndTag.Length - TagSize</c> bytes. Throws <see cref="System.Security.Cryptography.CryptographicException" /> (or
    /// <c>AuthenticationTagMismatchException</c>) on tag mismatch.
    /// </summary>
    void Decrypt(ReadOnlySpan<byte> ciphertextAndTag, ReadOnlySpan<byte> nonce, Span<byte> plaintext);
}

/// <summary>
/// Encodes/decodes the compact streaming chunk frame <c>[ciphertextLen:int32 LE][nonce:NonceSize][ciphertext][tag:TagSize]</c> into caller-owned buffers, with no per-chunk heap
/// allocation. The nonce is supplied by the caller (a per-stream random prefix plus a per-chunk counter) so the codec is agnostic to how the nonce was produced.
/// </summary>
internal static class AeadChunkCodec
{
    /// <summary>Fixed-size length prefix preceding each chunk body.</summary>
    public const int LengthPrefixSize = 4;

    /// <summary>Bytes of framing overhead added to each plaintext chunk: length prefix + nonce + tag.</summary>
    public static int Overhead(IAeadStreamCryptor cryptor) => LengthPrefixSize + cryptor.NonceSize + cryptor.TagSize;

    /// <summary>
    /// Writes one chunk (length prefix, nonce, ciphertext, tag) for <paramref name="plaintextLength" /> plaintext bytes read into <paramref name="plaintext" /> at offset 0 into
    /// <paramref name="destination" /> and returns the total number of bytes written. <paramref name="nonce" /> is copied into the frame and used for encryption.
    /// </summary>
    public static int Encode(IAeadStreamCryptor cryptor, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> nonce, Span<byte> destination)
    {
        var nonceSize = cryptor.NonceSize;
        var plaintextLength = plaintext.Length;
        BinaryPrimitives.WriteInt32LittleEndian(destination[..LengthPrefixSize], plaintextLength);
        nonce.CopyTo(destination.Slice(LengthPrefixSize, nonceSize));
        cryptor.Encrypt(plaintext, nonce, destination.Slice(LengthPrefixSize + nonceSize, plaintextLength + cryptor.TagSize));
        return LengthPrefixSize + nonceSize + plaintextLength + cryptor.TagSize;
    }

    /// <summary>
    /// Decrypts one chunk body (<c>nonce + ciphertext + tag</c>) of length <paramref name="bodyLength" /> held in <paramref name="body" /> at offset 0 into
    /// <paramref name="plaintext" /> and returns the plaintext length. The length prefix must already have been consumed by the caller.
    /// </summary>
    public static int Decode(IAeadStreamCryptor cryptor, ReadOnlySpan<byte> body, int bodyLength, Span<byte> plaintext)
    {
        var nonceSize = cryptor.NonceSize;
        var ciphertextLength = bodyLength - nonceSize - cryptor.TagSize;
        cryptor.Decrypt(body.Slice(nonceSize, ciphertextLength + cryptor.TagSize), body[..nonceSize], plaintext[..ciphertextLength]);
        return ciphertextLength;
    }

    /// <summary>Ensures <paramref name="buffer" /> is rented from <see cref="ArrayPool{T}" /> and at least <paramref name="size" /> bytes; grows (return + re-rent) if too small.</summary>
    public static void EnsureCapacity(ref byte[]? buffer, int size)
    {
        if (buffer != null && buffer.Length >= size)
            return;

        if (buffer != null)
            ArrayPool<byte>.Shared.Return(buffer);

        buffer = ArrayPool<byte>.Shared.Rent(size);
    }

    /// <summary>
    /// Reads up to <paramref name="count" /> bytes into <paramref name="buffer" /> at offset 0, looping until <paramref name="count" /> bytes are read or the stream ends. Returns
    /// the number of bytes actually read (equal to <paramref name="count" /> on success, 0 on a clean end of stream, or a partial count if the stream ends mid-read). Robust
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
