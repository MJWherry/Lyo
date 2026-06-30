using System.Buffers;
using System.Buffers.Binary;
using Lyo.Common.Security;
using Lyo.Encryption.Exceptions;

namespace Lyo.Encryption.Streaming;

/// <summary>
/// Shared streaming chunk loops for the compact AEAD frame <c>[ciphertextLen:int32 LE][nonce][ciphertext][tag]</c>. Buffers are rented once per stream from
/// <see cref="ArrayPool{T}" /> and reused for every chunk, so total allocation is O(chunkSize) regardless of stream length. Used by both <see cref="EncryptionServiceBase" /> and
/// the two-key envelope service.
/// </summary>
internal static class AeadStreamProcessor
{
    /// <summary>Maximum allowed encrypted chunk size (200 MB) to prevent denial-of-service attacks via oversized length prefixes.</summary>
    public const int MaxEncryptedChunkSize = 200 * 1024 * 1024;

    /// <summary>Width of the per-chunk counter that occupies the trailing bytes of every stream nonce.</summary>
    private const int CounterSize = 4;

    /// <summary>
    /// Reads <paramref name="input" /> in <paramref name="effectiveChunkSize" /> chunks and writes compact encrypted frames to <paramref name="output" />. Each chunk's nonce is a
    /// per-stream random prefix (the leading <c>NonceSize - 4</c> bytes, drawn once) followed by a 4-byte little-endian chunk counter. This is stateless and lock-free: every
    /// streaming operation owns its own prefix and counter, so concurrent encryptions (even with the same key) never collide and there are no shared-state or KeyStore round-trips.
    /// </summary>
    public static async Task EncryptChunksAsync(
        Stream input, Stream output, IAeadStreamCryptor cryptor, int effectiveChunkSize, CancellationToken ct)
    {
        var nonceSize = cryptor.NonceSize;
        var readBuffer = ArrayPool<byte>.Shared.Rent(effectiveChunkSize);
        var dest = ArrayPool<byte>.Shared.Rent(effectiveChunkSize + AeadChunkCodec.Overhead(cryptor));

        // Random prefix once per stream; the trailing CounterSize bytes are overwritten per chunk with the
        // counter. Uniqueness is guaranteed within a stream by the counter and across streams by the prefix.
        var nonce = new byte[nonceSize];
        CryptographicRandom.Fill(nonce.AsSpan(0, nonceSize - CounterSize));
        try {
            long chunkIndex = 0;
            int bytesRead;
            while ((bytesRead = await input.ReadAsync(readBuffer, 0, Math.Min(effectiveChunkSize, readBuffer.Length), ct).ConfigureAwait(false)) > 0) {
                ct.ThrowIfCancellationRequested();
                if (chunkIndex > uint.MaxValue)
                    throw new InvalidOperationException($"Stream exceeds the maximum of {(long)uint.MaxValue + 1} chunks for a single nonce prefix.");

                BinaryPrimitives.WriteUInt32LittleEndian(nonce.AsSpan(nonceSize - CounterSize, CounterSize), (uint)chunkIndex);
                var total = AeadChunkCodec.Encode(cryptor, readBuffer.AsSpan(0, bytesRead), nonce, dest);
                await output.WriteAsync(dest, 0, total, ct).ConfigureAwait(false);
                chunkIndex++;
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(readBuffer);
            ArrayPool<byte>.Shared.Return(dest);
        }
    }

    /// <summary>Reads compact encrypted frames from <paramref name="input" />, decrypts each with <paramref name="cryptor" />, and writes plaintext to <paramref name="output" />.</summary>
    public static async Task DecryptChunksAsync(Stream input, Stream output, IAeadStreamCryptor cryptor, CancellationToken ct)
    {
        var overheadPerChunk = cryptor.NonceSize + cryptor.TagSize;
        var lengthBuffer = ArrayPool<byte>.Shared.Rent(AeadChunkCodec.LengthPrefixSize);
        byte[]? bodyBuffer = null;
        byte[]? plainBuffer = null;
        try {
            int lengthRead;
            while ((lengthRead = await AeadChunkCodec.ReadAtLeastAsync(input, lengthBuffer, AeadChunkCodec.LengthPrefixSize, ct).ConfigureAwait(false)) ==
                   AeadChunkCodec.LengthPrefixSize) {
                ct.ThrowIfCancellationRequested();
                var ciphertextLength = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
                if (ciphertextLength <= 0)
                    throw new InvalidDataException($"Invalid chunk length: {ciphertextLength}. Chunk length must be positive.");

                if (ciphertextLength > MaxEncryptedChunkSize) {
                    throw new InvalidDataException(
                        $"Invalid chunk length: {ciphertextLength} bytes. Maximum allowed: {MaxEncryptedChunkSize} bytes ({MaxEncryptedChunkSize / (1024 * 1024)} MB).");
                }

                var bodyLength = ciphertextLength + overheadPerChunk;

                // Validate against the FULL on-wire body (nonce + ciphertext + tag), not just the ciphertext length.
                if (input.CanSeek) {
                    var remainingBytes = input.Length - input.Position;
                    if (remainingBytes < bodyLength) {
                        throw new InvalidDataException(
                            $"Invalid encrypted data format: chunk body ({bodyLength} bytes) exceeds remaining stream size ({remainingBytes} bytes).");
                    }
                }

                AeadChunkCodec.EnsureCapacity(ref bodyBuffer, bodyLength);
                AeadChunkCodec.EnsureCapacity(ref plainBuffer, ciphertextLength);
                if (await AeadChunkCodec.ReadAtLeastAsync(input, bodyBuffer!, bodyLength, ct).ConfigureAwait(false) != bodyLength)
                    throw new EndOfStreamException("Unexpected end of stream while reading encrypted chunk.");

                int plaintextLength;
                try {
                    plaintextLength = AeadChunkCodec.Decode(cryptor, bodyBuffer.AsSpan(0, bodyLength), bodyLength, plainBuffer);
                }
                catch (DecryptionFailedException) {
                    throw;
                }
                catch (Exception ex) {
                    throw new DecryptionFailedException("Failed to decrypt data chunk. Possible causes: wrong key, corrupted data, or authentication failure.", ex);
                }

                await output.WriteAsync(plainBuffer, 0, plaintextLength, ct).ConfigureAwait(false);
            }

            if (lengthRead != 0)
                throw new EndOfStreamException("Unexpected end of stream while reading chunk length.");
        }
        finally {
            ArrayPool<byte>.Shared.Return(lengthBuffer);
            if (bodyBuffer != null)
                ArrayPool<byte>.Shared.Return(bodyBuffer);

            if (plainBuffer != null)
                ArrayPool<byte>.Shared.Return(plainBuffer);
        }
    }
}
