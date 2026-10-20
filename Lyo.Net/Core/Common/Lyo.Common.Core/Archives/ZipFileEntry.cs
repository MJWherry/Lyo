namespace Lyo.Common.Core.Archives;

/// <summary>One entry listed by <see cref="ZipFileReader" />.</summary>
public sealed record ZipFileEntry
{
    /// <summary>Entry name as stored in the archive, using <c>/</c> separators.</summary>
    public required string Name { get; init; }

    /// <summary>Uncompressed size in bytes. Zero for a directory entry.</summary>
    public long Length { get; init; }

    /// <summary>Compressed size in bytes.</summary>
    public long CompressedLength { get; init; }

    /// <summary>Last-write time stored on the entry.</summary>
    public DateTimeOffset LastWriteTime { get; init; }

    /// <summary>Entry comment. Null when the archive has none.</summary>
    public string? Comment { get; init; }

    /// <summary>Unix permission bits from the high 16 bits of the external attributes. Null when those bits are zero.</summary>
    public int? UnixMode { get; init; }

    /// <summary>True when the entry is a directory.</summary>
    public bool IsDirectory { get; init; }
}
