using Lyo.Common.Metadata.Records;

namespace Lyo.FileStorage;

/// <summary>Zip built by <see cref="Abstractions.IFileStorageArchiveService" />. Disposing <see cref="Stream" /> drops the backing temp session.</summary>
public sealed class FileStorageArchive
{
    /// <summary>Readable zip bytes. Dispose after the HTTP response, or when the download is abandoned.</summary>
    public Stream Stream { get; }

    /// <summary>Sanitized download name, including <c>.zip</c>.</summary>
    public string FileName { get; }

    /// <summary>Always <c>application/zip</c>.</summary>
    public string ContentType { get; } = FileTypeInfo.Zip.MimeType;

    /// <summary>Zip size in bytes.</summary>
    public long Length { get; }

    internal FileStorageArchive(Stream stream, string fileName, long length)
    {
        Stream = stream;
        FileName = fileName;
        Length = length;
    }
}
