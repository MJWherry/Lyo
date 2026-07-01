using System.Buffers.Binary;
using Lyo.Encryption.Utilities;
using Lyo.Streams;

namespace Lyo.Encryption.Rsa;

/// <summary>
/// Reproduces the legacy per-chunk stream framing (<c>[formatVersion:1][algorithmId:1][reserved:2]</c> followed by length-prefixed ciphertext chunks) used by the RSA
/// encrypt/decrypt services so the on-disk <c>.rsa</c> stream/file format stays byte-compatible after the split out of <see cref="EncryptionServiceBase" />. The per-chunk transform
/// is supplied by the caller (RSA encrypt/decrypt of a buffer slice).
/// </summary>
internal static class RsaStreamCodec
{
    // Maximum allowed encrypted chunk size (200 MB) to prevent denial-of-service attacks while accounting for encryption overhead.
    private const int MaxEncryptedChunkSize = 200 * 1024 * 1024;

    public static async Task EncryptAsync(
        Stream input,
        Stream output,
        byte algorithmId,
        byte formatVersion,
        int chunkSize,
        Func<byte[], int, int, byte[]> encryptChunk,
        CancellationToken ct)
    {
        var effectiveChunkSize = chunkSize <= 0 ? StreamChunkSizeHelper.DetermineChunkSize(input) : chunkSize;
        await output.WriteAsync(new[] { formatVersion }, 0, 1, ct).ConfigureAwait(false);
        await output.WriteAsync(new[] { algorithmId }, 0, 1, ct).ConfigureAwait(false);
        await output.WriteAsync(new byte[2], 0, 2, ct).ConfigureAwait(false);
        var buffer = BufferPool.Rent(effectiveChunkSize);
        try {
            int bytesRead;
            while ((bytesRead = await input.ReadAsync(buffer, 0, Math.Min(effectiveChunkSize, buffer.Length), ct).ConfigureAwait(false)) > 0) {
                ct.ThrowIfCancellationRequested();
                var encryptedChunk = encryptChunk(buffer, 0, bytesRead);
                var lengthBytes = new byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, encryptedChunk.Length);
                await output.WriteAsync(lengthBytes, 0, 4, ct).ConfigureAwait(false);
                await output.WriteAsync(encryptedChunk, 0, encryptedChunk.Length, ct).ConfigureAwait(false);
            }
        }
        finally {
            BufferPool.Return(buffer);
        }
    }

    public static async Task DecryptAsync(Stream input, Stream output, byte algorithmId, byte formatVersion, Func<byte[], int, int, byte[]> decryptChunk, CancellationToken ct)
    {
        var headerBuffer = BufferPool.RentExact(4, true);
        try {
            var headerBytesRead = await input.ReadAsync(headerBuffer, 0, 4, ct).ConfigureAwait(false);
            if (headerBytesRead != 4)
                throw new InvalidDataException("Invalid encrypted stream format: insufficient data for header.");

            var firstByte = headerBuffer[0];
            if (firstByte != formatVersion)
                throw new InvalidDataException($"Invalid encrypted stream format: expected format version {formatVersion}, got {firstByte}.");

            var streamAlgorithmId = headerBuffer[1];
            if (streamAlgorithmId != algorithmId) {
                throw new InvalidDataException(
                    $"Stream algorithm ID mismatch. Expected {algorithmId} ({(EncryptionAlgorithm)algorithmId}), got {streamAlgorithmId} ({(EncryptionAlgorithm)streamAlgorithmId}).");
            }

            var lengthBuffer = BufferPool.RentExact(4, true);
            try {
                if (await input.ReadAsync(lengthBuffer, 0, 4, ct).ConfigureAwait(false) != 4)
                    return; // No chunks after header

                while (true) {
                    ct.ThrowIfCancellationRequested();
                    var chunkLength = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
                    if (chunkLength <= 0)
                        throw new InvalidDataException($"Invalid chunk length: {chunkLength}. Chunk length must be positive.");

                    if (chunkLength > MaxEncryptedChunkSize) {
                        throw new InvalidDataException(
                            $"Invalid chunk length: {chunkLength} bytes. Maximum allowed: {MaxEncryptedChunkSize} bytes ({MaxEncryptedChunkSize / (1024 * 1024)} MB).");
                    }

                    if (input.CanSeek) {
                        var remainingBytes = input.Length - input.Position;
                        if (remainingBytes < chunkLength) {
                            throw new InvalidDataException(
                                $"Invalid encrypted data format: chunk length ({chunkLength} bytes) exceeds remaining stream size ({remainingBytes} bytes).");
                        }
                    }

                    var encryptedChunk = BufferPool.Rent(chunkLength);
                    try {
                        var totalRead = 0;
                        while (totalRead < chunkLength) {
                            ct.ThrowIfCancellationRequested();
                            var bytesRead = await input.ReadAsync(encryptedChunk, totalRead, chunkLength - totalRead, ct).ConfigureAwait(false);
                            if (bytesRead == 0)
                                throw new EndOfStreamException("Unexpected end of encrypted stream.");

                            totalRead += bytesRead;
                        }

                        var decryptedChunk = decryptChunk(encryptedChunk, 0, chunkLength);
                        await output.WriteAsync(decryptedChunk, 0, decryptedChunk.Length, ct).ConfigureAwait(false);
                    }
                    finally {
                        BufferPool.Return(encryptedChunk);
                    }

                    if (await input.ReadAsync(lengthBuffer, 0, 4, ct).ConfigureAwait(false) != 4)
                        break; // End of stream
                }
            }
            finally {
                BufferPool.Return(lengthBuffer);
            }
        }
        finally {
            BufferPool.Return(headerBuffer);
        }
    }
}