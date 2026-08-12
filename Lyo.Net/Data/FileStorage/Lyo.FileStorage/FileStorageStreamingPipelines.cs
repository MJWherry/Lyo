using System.IO.Pipelines;
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Encryption;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.TwoKey;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;
using Lyo.Hashing;
using Lyo.Streams;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage;

/// <summary>
/// Encapsulates encode/decode streaming stages shared by every storage backend—single-pass compress-then-encrypt uploads and pipelined decrypt/decompress reads. Persisted
/// ciphertext dimensions and headers are read through <see cref="IFileStoragePhysicalIO" />.
/// </summary>
internal sealed class FileStorageStreamingPipelines
{
    private readonly ICompressionService? _compressionService;
    private readonly int _copyToBufferSizeBytes;
    private readonly ILogger _logger;
    private readonly FileStorageServiceBaseOptions _options;
    private readonly IFileStoragePhysicalIO _physicalIO;
    private readonly ITwoKeyEncryptionService? _twoKeyEncryptionService;

    /// <summary>Initializes pipelines with polymorphic blob I/O and optional crypto services.</summary>
    internal FileStorageStreamingPipelines(
        IFileStoragePhysicalIO physicalIO,
        ICompressionService? compressionService,
        ITwoKeyEncryptionService? twoKeyEncryptionService,
        FileStorageServiceBaseOptions options,
        ILogger logger,
        int copyToBufferSizeBytes)
    {
        _physicalIO = physicalIO;
        _compressionService = compressionService;
        _twoKeyEncryptionService = twoKeyEncryptionService;
        _options = options;
        _logger = logger;
        _copyToBufferSizeBytes = copyToBufferSizeBytes;
    }

    /// <summary>Disposes <paramref name="stream" /> asynchronously when supported; otherwise synchronously.</summary>
    internal static Task DisposeStreamAsync(Stream? stream)
    {
        if (stream == null)
            return Task.CompletedTask;
#if NET5_0_OR_GREATER
        return stream.DisposeAsync().AsTask();
#else
        stream.Dispose();
        return Task.CompletedTask;
#endif
    }

    /// <summary>
    /// Writes decrypted and/or decompressed plaintext into <paramref name="plainWriter" /> based on <paramref name="metadata" /> compression and encryption flags, leveraging
    /// <see cref="Pipe" /> backpressure to avoid buffering entire files in memory.
    /// </summary>
    internal async Task RunStreamingDecodePipelineAsync(
        Stream storageStream,
        FileStoreResult metadata,
        PipeWriter plainWriter,
        int? chunkSize,
        CompressionAlgorithm? compressionAlgorithmOverride,
        CancellationToken ct)
    {
        CompressionAlgorithm? decompressAlgorithm = null;
        if (metadata.IsCompressed) {
            OperationHelpers.ThrowIfNull(
                _compressionService,
                $"File {metadata.Id} is compressed but no compression service is configured. " +
                "Provide an ICompressionService when creating FileStorageService to decompress compressed files.");

            decompressAlgorithm = FileStorageCompression.ResolveDecompressionAlgorithm(
                metadata, compressionAlgorithmOverride, _options.DecompressionAlgorithmOverride, _compressionService, _logger, metadata.Id);
        }

        using (storageStream) {
            if (metadata.IsEncrypted && metadata.IsCompressed) {
                OperationHelpers.ThrowIfNull(
                    _twoKeyEncryptionService,
                    $"File {metadata.Id} is encrypted but no encryption service is configured. " +
                    "Provide an ITwoKeyEncryptionService instance when creating FileStorageService to decrypt encrypted files.");

                if (storageStream.CanSeek)
                    storageStream.Position = 0;

                var pipeCompressed = new Pipe();
                await Task.WhenAll(
                        DecryptStorageIntoCompressedPipeAsync(storageStream, pipeCompressed, _twoKeyEncryptionService!, ct),
                        DecompressCompressedPipeToPlainAsync(pipeCompressed, plainWriter, decompressAlgorithm!, chunkSize, ct))
                    .ConfigureAwait(false);

                _logger.LogDebug("Stream pipeline complete for file {FileId} (decrypt + decompress)", metadata.Id);
                return;
            }

            if (metadata.IsEncrypted) {
                OperationHelpers.ThrowIfNull(
                    _twoKeyEncryptionService,
                    $"File {metadata.Id} is encrypted but no encryption service is configured. " +
                    "Provide an ITwoKeyEncryptionService instance when creating FileStorageService to decrypt encrypted files.");

                if (storageStream.CanSeek)
                    storageStream.Position = 0;

                try {
                    using (var plainOut = plainWriter.AsStream(true))
                        await _twoKeyEncryptionService!.DecryptToStreamAsync(storageStream, plainOut, null, null, ct).ConfigureAwait(false);

                    await plainWriter.CompleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex) {
                    await plainWriter.CompleteAsync(ex).ConfigureAwait(false);
                    throw;
                }

                _logger.LogDebug("Stream pipeline complete for file {FileId} (decrypt only)", metadata.Id);
                return;
            }

            if (metadata.IsCompressed) {
                if (storageStream.CanSeek)
                    storageStream.Position = 0;

                try {
                    using (var plainOut = plainWriter.AsStream(true))
                        await _compressionService!.Resolver.DecompressAsync(storageStream, plainOut, decompressAlgorithm!, chunkSize, ct).ConfigureAwait(false);

                    await plainWriter.CompleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex) {
                    await plainWriter.CompleteAsync(ex).ConfigureAwait(false);
                    throw;
                }

                _logger.LogDebug("Stream pipeline complete for file {FileId} (decompress only)", metadata.Id);
                return;
            }

            await plainWriter.CompleteAsync(
                    new InvalidOperationException(
                        $"File {metadata.Id} was expected to be encrypted and/or compressed for streaming decode, but metadata flags do not indicate either."))
                .ConfigureAwait(false);
        }
    }

    /// <summary>Executes the single-pass pipeline that compresses plaintext, pipes the compressed stream, and encrypts concurrently for memory-efficient saves.</summary>
    internal async Task<CompressEncryptPipelineResult> SaveWithCompressEncryptPipelineAsync(
        Stream inputStream,
        Stream outputStream,
        Guid fileId,
        string keyId,
        string? normalizedPathPrefix,
        int chunkSize,
        long originalSize,
        CompressionAlgorithm compressionAlgorithm,
        CancellationToken ct)
    {
        OperationHelpers.ThrowIfNull(_compressionService, "Compression service required for pipeline.");
        OperationHelpers.ThrowIfNull(_twoKeyEncryptionService, "Encryption service required for pipeline.");
        var compressionExt = compressionAlgorithm.Extension;
        var fileExtension = _twoKeyEncryptionService.FileExtension;
        var sourceFileName = fileId + fileExtension;
        var pipe = new Pipe();
        var hashAlg = _options.HashAlgorithm;
        using var compressedHashAlgo = hashAlg.Create();
        var pipeWriterStream = pipe.Writer.AsStream(true);
        using var countingStream = new CountingStream(pipeWriterStream);
        using var compressedHashStream = new HashingStream(countingStream, compressedHashAlgo);
        using var inputForHash = new HashingStream(inputStream, hashAlg.Create());
        using var encryptedHashAlgo = hashAlg.Create();
        using var encryptedHashStream = new HashingStream(outputStream, encryptedHashAlgo);
        var pipeReaderStream = pipe.Reader.AsStream(true);
        var compressionTask = RunCompressIntoPipeWriterAsync(_compressionService.Resolver, compressionAlgorithm, inputForHash, compressedHashStream, chunkSize, pipe, ct);
        var encryptionTask = RunEncryptFromPipeReaderAsync(_twoKeyEncryptionService!, pipeReaderStream, encryptedHashStream, keyId, chunkSize, ct);
        await Task.WhenAll(compressionTask, encryptionTask).ConfigureAwait(false);
        await encryptedHashStream.FlushAsync(ct).ConfigureAwait(false);
        await outputStream.FlushAsync(ct).ConfigureAwait(false);
        await DisposeStreamAsync(outputStream).ConfigureAwait(false);
        var compressedSize = countingStream.BytesWritten;
        var compressedHash = compressedHashStream.GetHash();
        var encryptedHash = encryptedHashStream.GetHash();
        var encryptedSize = await _physicalIO.GetStorageSizeAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
        var dataEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineDekAlgorithm(_twoKeyEncryptionService);
        var keyEncryptionKeyAlgorithm = EncryptionServiceExtensions.DetermineKekAlgorithm(_twoKeyEncryptionService);
        var headerInfo = await _physicalIO.ExtractEncryptionHeaderAsync(fileId, fileExtension, normalizedPathPrefix, ct).ConfigureAwait(false);
        var dataEncryptionKeyId = headerInfo.DataEncryptionKeyId ?? keyId;
        var dataEncryptionKeyVersion = headerInfo.DataEncryptionKeyVersion;
        var keyEncryptionKeySalt = dataEncryptionKeyVersion != null ? _twoKeyEncryptionService.GetSaltForVersion(dataEncryptionKeyId, dataEncryptionKeyVersion) : null;
        _logger.LogDebug(
            "Compressed and encrypted file {FileId} in single pass: {OriginalSize} -> {CompressedSize} -> {EncryptedSize} bytes", fileId, originalSize, compressedSize,
            encryptedSize);

        return new(
            inputForHash.GetHash(), fileExtension, sourceFileName, compressedSize, compressedHash, compressionAlgorithm, encryptedHash, headerInfo.EncryptedDataEncryptionKey,
            dataEncryptionKeyId, dataEncryptionKeyVersion, keyEncryptionKeySalt, encryptedSize, dataEncryptionKeyAlgorithm, keyEncryptionKeyAlgorithm,
            headerInfo.DekKeyMaterialBytes);
    }

    private static async Task RunCompressIntoPipeWriterAsync(
        ICompressionResolver compressionResolver,
        CompressionAlgorithm algorithm,
        HashingStream inputForHash,
        HashingStream compressedHashStream,
        int chunkSize,
        Pipe pipe,
        CancellationToken ct)
    {
        try {
            await compressionResolver.CompressAsync(inputForHash, compressedHashStream, algorithm, chunkSize, ct: ct).ConfigureAwait(false);
            await compressedHashStream.FlushAsync(ct).ConfigureAwait(false);
            await inputForHash.FlushAsync(ct).ConfigureAwait(false);
            await pipe.Writer.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            await pipe.Writer.CompleteAsync(ex).ConfigureAwait(false);
            throw;
        }
    }

    private static Task RunEncryptFromPipeReaderAsync(
        ITwoKeyEncryptionService encryptionService,
        Stream pipeReader,
        Stream encryptedHashStream,
        string keyId,
        int chunkSize,
        CancellationToken ct)
        => encryptionService.EncryptToStreamAsync(pipeReader, encryptedHashStream, keyId, null, chunkSize, ct);

    private static async Task DecryptStorageIntoCompressedPipeAsync(Stream storageStream, Pipe pipeCompressed, ITwoKeyEncryptionService twoKey, CancellationToken ct)
    {
        try {
            using (var w = pipeCompressed.Writer.AsStream(true))
                await twoKey.DecryptToStreamAsync(storageStream, w, null, null, ct).ConfigureAwait(false);

            await pipeCompressed.Writer.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            await pipeCompressed.Writer.CompleteAsync(ex).ConfigureAwait(false);
            throw;
        }
    }

    private async Task DecompressCompressedPipeToPlainAsync(Pipe pipeCompressed, PipeWriter plainWriter, CompressionAlgorithm algorithm, int? chunkSize, CancellationToken ct)
    {
        try {
            using var compressedRead = pipeCompressed.Reader.AsStream(true);
            using var plainOut = plainWriter.AsStream(true);
            await _compressionService!.Resolver.DecompressAsync(compressedRead, plainOut, algorithm, chunkSize, ct).ConfigureAwait(false);
            await plainWriter.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            await plainWriter.CompleteAsync(ex).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Captured hashes, sizes, crypto metadata, and filenames produced by <see cref="SaveWithCompressEncryptPipelineAsync" />.</summary>
    internal sealed record CompressEncryptPipelineResult(
        byte[]? OriginalHash,
        string FileExtension,
        string SourceFileName,
        long CompressedSize,
        byte[]? CompressedHash,
        CompressionAlgorithm? CompressionAlgorithm,
        byte[]? EncryptedHash,
        byte[]? EncryptedDataEncryptionKey,
        string? DataEncryptionKeyId,
        string? DataEncryptionKeyVersion,
        byte[]? KeyEncryptionKeySalt,
        long EncryptedSize,
        EncryptionAlgorithm? DataEncryptionKeyAlgorithm,
        EncryptionAlgorithm? KeyEncryptionKeyAlgorithm,
        byte DekKeyMaterialBytes);
}