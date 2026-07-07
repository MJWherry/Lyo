using System.Buffers;
using System.Buffers.Binary;
using Lyo.Encryption.Exceptions;

namespace Lyo.Encryption.Streaming;

/// <summary>
/// Shared streaming chunk loops for the compact AEAD frames (see <see cref="AeadChunkCodec" />). Buffers are rented once per stream from <see cref="ArrayPool{T}" /> and
/// reused for every chunk, so total allocation is O(chunkSize) regardless of stream length. Buffers that held plaintext are zeroed before being returned to the shared pool. Used by
/// both <see cref="EncryptionServiceBase" /> and the two-key envelope service.
/// </summary>
internal static class AeadStreamProcessor
{
    /// <summary>Maximum allowed encrypted chunk size (200 MB) to prevent denial-of-service attacks via oversized length prefixes.</summary>
    public const int MaxEncryptedChunkSize = 200 * 1024 * 1024;

    /// <summary>Width of the per-chunk counter that occupies the trailing bytes of every stream nonce.</summary>
    public const int CounterSize = 4;

    /// <summary>
    /// Encrypt loop: reads <paramref name="input" /> in <paramref name="effectiveChunkSize" /> chunks and writes compact frames to <paramref name="output" />. Each chunk's
    /// nonce is <paramref name="noncePrefix" /> (drawn once per stream and persisted in the stream header) followed by a 4-byte little-endian chunk counter; the nonce is never
    /// written to the wire, so the decryptor must derive the identical sequence — reordered, replayed, or dropped chunks fail authentication. The last chunk is flagged as final
    /// (detecting truncation) and every chunk authenticates <paramref name="aadNonFinal" /> / <paramref name="aadFinal" /> (stream header + final-flag byte + caller AAD).
    /// </summary>
    public static async Task EncryptChunksAsync(
        Stream input,
        Stream output,
        IAeadStreamCryptor cryptor,
        int effectiveChunkSize,
        byte[] noncePrefix,
        byte[] aadNonFinal,
        byte[] aadFinal,
        CancellationToken ct)
    {
        var nonceSize = cryptor.NonceSize;
        var current = ArrayPool<byte>.Shared.Rent(effectiveChunkSize);
        var next = ArrayPool<byte>.Shared.Rent(effectiveChunkSize);
        var dest = ArrayPool<byte>.Shared.Rent(effectiveChunkSize + AeadChunkCodec.Overhead(cryptor));
        var nonce = new byte[nonceSize];
        noncePrefix.AsSpan(0, nonceSize - CounterSize).CopyTo(nonce);
        try {
            // One chunk of read-ahead so the final chunk can be flagged: `current` is only emitted once we know
            // whether another read produced data. An empty input still emits one zero-length final chunk so
            // truncation of the whole payload is detectable.
            var currentLength = await AeadChunkCodec.ReadAtLeastAsync(input, current, effectiveChunkSize, ct).ConfigureAwait(false);
            long chunkIndex = 0;
            while (true) {
                ct.ThrowIfCancellationRequested();
                var nextLength = currentLength == 0 ? 0 : await AeadChunkCodec.ReadAtLeastAsync(input, next, effectiveChunkSize, ct).ConfigureAwait(false);
                var isFinal = nextLength == 0;
                if (chunkIndex > uint.MaxValue)
                    throw new InvalidOperationException($"Stream exceeds the maximum of {(long)uint.MaxValue + 1} chunks for a single nonce prefix.");

                BinaryPrimitives.WriteUInt32LittleEndian(nonce.AsSpan(nonceSize - CounterSize, CounterSize), (uint)chunkIndex);
                var total = AeadChunkCodec.Encode(cryptor, current.AsSpan(0, currentLength), nonce, isFinal ? aadFinal : aadNonFinal, isFinal, dest);
                await output.WriteAsync(dest, 0, total, ct).ConfigureAwait(false);
                chunkIndex++;
                if (isFinal)
                    break;

                (current, next) = (next, current);
                currentLength = nextLength;
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(current, true);
            ArrayPool<byte>.Shared.Return(next, true);
            ArrayPool<byte>.Shared.Return(dest);
        }
    }

    /// <summary>
    /// Decrypt loop: reads compact frames from <paramref name="input" />, deriving each chunk's nonce from <paramref name="noncePrefix" /> plus a local counter (the wire
    /// carries no nonce) and authenticating <paramref name="aadNonFinal" /> / <paramref name="aadFinal" /> per chunk. Throws if the stream ends before the final-flagged chunk
    /// (truncation) or contains data after it (extension).
    /// </summary>
    public static async Task DecryptChunksAsync(
        Stream input,
        Stream output,
        IAeadStreamCryptor cryptor,
        byte[] noncePrefix,
        byte[] aadNonFinal,
        byte[] aadFinal,
        CancellationToken ct)
    {
        var nonceSize = cryptor.NonceSize;
        var tagSize = cryptor.TagSize;
        var lengthBuffer = ArrayPool<byte>.Shared.Rent(AeadChunkCodec.LengthPrefixSize);
        byte[]? bodyBuffer = null;
        byte[]? plainBuffer = null;
        var nonce = new byte[nonceSize];
        noncePrefix.AsSpan(0, nonceSize - CounterSize).CopyTo(nonce);
        try {
            var sawFinalChunk = false;
            long chunkIndex = 0;
            while (true) {
                ct.ThrowIfCancellationRequested();
                var lengthRead = await AeadChunkCodec.ReadAtLeastAsync(input, lengthBuffer, AeadChunkCodec.LengthPrefixSize, ct).ConfigureAwait(false);
                if (lengthRead == 0) {
                    if (!sawFinalChunk)
                        throw new InvalidDataException("Encrypted stream is truncated: it ended before the final chunk marker.");

                    break;
                }

                if (sawFinalChunk)
                    throw new InvalidDataException("Invalid encrypted stream: data found after the final chunk marker.");

                if (lengthRead != AeadChunkCodec.LengthPrefixSize)
                    throw new EndOfStreamException("Unexpected end of stream while reading chunk length.");

                var lengthAndFlag = BinaryPrimitives.ReadUInt32LittleEndian(lengthBuffer);
                var isFinal = (lengthAndFlag & AeadChunkCodec.FinalChunkFlag) != 0;
                var ciphertextLength = (int)(lengthAndFlag & ~AeadChunkCodec.FinalChunkFlag);
                if (ciphertextLength == 0 && !isFinal)
                    throw new InvalidDataException("Invalid chunk length: 0. Only the final chunk may be empty.");

                if (ciphertextLength > MaxEncryptedChunkSize) {
                    throw new InvalidDataException(
                        $"Invalid chunk length: {ciphertextLength} bytes. Maximum allowed: {MaxEncryptedChunkSize} bytes ({MaxEncryptedChunkSize / (1024 * 1024)} MB).");
                }

                if (chunkIndex > uint.MaxValue)
                    throw new InvalidDataException($"Encrypted stream exceeds the maximum of {(long)uint.MaxValue + 1} chunks for a single nonce prefix.");

                var bodyLength = ciphertextLength + tagSize;
                if (input.CanSeek) {
                    var remainingBytes = input.Length - input.Position;
                    if (remainingBytes < bodyLength)
                        throw new InvalidDataException($"Invalid encrypted data format: chunk body ({bodyLength} bytes) exceeds remaining stream size ({remainingBytes} bytes).");
                }

                AeadChunkCodec.EnsureCapacity(ref bodyBuffer, bodyLength);
                AeadChunkCodec.EnsureCapacity(ref plainBuffer, Math.Max(ciphertextLength, 1), true);
                if (await AeadChunkCodec.ReadAtLeastAsync(input, bodyBuffer, bodyLength, ct).ConfigureAwait(false) != bodyLength)
                    throw new EndOfStreamException("Unexpected end of stream while reading encrypted chunk.");

                BinaryPrimitives.WriteUInt32LittleEndian(nonce.AsSpan(nonceSize - CounterSize, CounterSize), (uint)chunkIndex);
                int plaintextLength;
                try {
                    plaintextLength = AeadChunkCodec.Decode(cryptor, bodyBuffer.AsSpan(0, bodyLength), bodyLength, nonce, isFinal ? aadFinal : aadNonFinal, plainBuffer);
                }
                catch (DecryptionFailedException) {
                    throw;
                }
                catch (Exception ex) {
                    throw new DecryptionFailedException(
                        "Failed to decrypt data chunk. Possible causes: wrong key, corrupted data, reordered or replayed chunks, mismatched associated data, or authentication failure.",
                        ex);
                }

                if (plaintextLength > 0)
                    await output.WriteAsync(plainBuffer, 0, plaintextLength, ct).ConfigureAwait(false);

                chunkIndex++;
                sawFinalChunk = isFinal;
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(lengthBuffer);
            if (bodyBuffer != null)
                ArrayPool<byte>.Shared.Return(bodyBuffer);

            if (plainBuffer != null)
                ArrayPool<byte>.Shared.Return(plainBuffer, true);
        }
    }
}
