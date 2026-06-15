using Lyo.Common.Records;

namespace Lyo.Compression.Models;

/// <summary>Inputs for <see cref="ICompressionAlgorithmSelector" /> when choosing a compression algorithm on write.</summary>
public sealed record CompressionSelectionContext
{
    public required long ByteLength { get; init; }

    /// <summary>Resolved MIME type (same value stored in file metadata).</summary>
    public string? ContentType { get; init; }

    public string? OriginalFileName { get; init; }

    public string? TenantId { get; init; }

    /// <summary>Resolved from <see cref="ContentType" /> then <see cref="OriginalFileName" />.</summary>
    public FileTypeInfo FileType
    {
        get {
            var fromMime = FileTypeInfo.FromMimeType(ContentType);
            return fromMime != FileTypeInfo.Unknown ? fromMime : FileTypeInfo.FromFilePath(OriginalFileName);
        }
    }
}
