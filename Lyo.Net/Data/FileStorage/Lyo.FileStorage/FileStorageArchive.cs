using Lyo.Common.Records;

namespace Lyo.FileStorage;

/// <summary>A zip produced by <see cref="Abstractions.IFileStorageArchiveService" />. Disposing <see cref="Stream" /> deletes the backing temp session.</summary>
public sealed class FileStorageArchive
{
    /// <summary>Readable zip bytes. Dispose after the HTTP response (or when abandoning the download).</summary>
    public Stream Stream { get; }

    /// <summary>Sanitized download file name, including <c>.zip</c>.</summary>
    public string FileName { get; }

    /// <summary>Always <c>application/zip</c>.</summary>
    public string ContentType { get; } = FileTypeInfo.Zip.MimeType;

    /// <summary>Zip length in bytes.</summary>
    public long Length { get; }

    internal FileStorageArchive(Stream stream, string fileName, long length)
    {
        Stream = stream;
        FileName = fileName;
        Length = length;
    }
}
