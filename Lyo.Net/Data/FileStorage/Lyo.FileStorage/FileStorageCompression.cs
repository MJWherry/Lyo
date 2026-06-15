using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Models;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage;

/// <summary>Compression algorithm resolution for file storage save and read paths.</summary>
internal static class FileStorageCompression
{
    internal static CompressionSelectionContext BuildSelectionContext(
        long byteLength,
        string? contentType,
        string? originalFileName,
        string? tenantId)
        => new() {
            ByteLength = byteLength,
            ContentType = contentType,
            OriginalFileName = originalFileName,
            TenantId = tenantId
        };

    internal static (bool ShouldCompress, CompressionAlgorithm? Algorithm) ResolveForSave(
        bool compressRequested,
        CompressionSelectionContext context,
        ICompressionService? compressionService,
        ILogger logger)
    {
        if (!compressRequested)
            return (false, null);

        OperationHelpers.ThrowIfNull(
            compressionService,
            "Compression was requested but no compression service is configured. Provide an ICompressionService instance when creating FileStorageService.");

        var selection = compressionService.ResolveForCompress(context);
        if (!selection.ShouldCompress) {
            logger.LogDebug("Compression policy declined compress for content type {ContentType}, size {Size}", context.ContentType, context.ByteLength);
            return (false, null);
        }

        OperationHelpers.ThrowIfNull(selection.Algorithm, "Compression policy selected compress but did not specify an algorithm.");
        return (true, selection.Algorithm);
    }

    internal static CompressionAlgorithm ResolveDecompressionAlgorithm(
        FileStoreResult metadata,
        CompressionAlgorithm? perCallOverride,
        CompressionAlgorithm? optionsOverride,
        ICompressionService? compressionService,
        ILogger logger,
        Guid fileId)
    {
        if (perCallOverride is { } callOverride) {
            if (metadata.CompressionAlgorithm is { } meta && meta != callOverride)
                logger.LogInformation(
                    "File {FileId} decompression algorithm overridden per-call: metadata {MetadataAlgorithm}, using {OverrideAlgorithm}",
                    fileId, meta.Name, callOverride.Name);

            return callOverride;
        }

        if (optionsOverride is { } optOverride) {
            if (metadata.CompressionAlgorithm is { } meta && meta != optOverride)
                logger.LogInformation(
                    "File {FileId} decompression algorithm overridden by options: metadata {MetadataAlgorithm}, using {OverrideAlgorithm}",
                    fileId, meta.Name, optOverride.Name);

            return optOverride;
        }

        if (metadata.CompressionAlgorithm is { } stored)
        {
            if (compressionService != null && stored != compressionService.Algorithm)
                logger.LogDebug(
                    "File {FileId} decompressing with metadata algorithm {MetadataAlgorithm} (configured default {DefaultAlgorithm})",
                    fileId, stored.Name, compressionService.Algorithm.Name);

            return stored;
        }

        OperationHelpers.ThrowIfNull(
            compressionService,
            $"File {fileId} is compressed but metadata does not specify CompressionAlgorithm and no compression service is configured.");

        return compressionService.Algorithm;
    }
}
