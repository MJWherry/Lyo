using System.Diagnostics;
using System.IO.Compression;

namespace Lyo.Common.Core.Archives;

/// <summary>Optional per-entry settings for <see cref="ZipFileBuilder" />. Unset values fall back to the builder.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ZipEntryOptions
{
    /// <summary>Compression for this entry. When null, <see cref="ZipFileBuilder.WithCompressionLevel" /> applies.</summary>
    public CompressionLevel? CompressionLevel { get; init; }

    /// <summary>Last-write time stored on the entry. When null, byte, stream, and text adds use UTC now.</summary>
    public DateTimeOffset? LastWriteTime { get; init; }

    /// <summary>Comment stored on this entry.</summary>
    public string? Comment { get; init; }

    /// <summary>Unix permission bits stored on this entry, such as <c>0x1ED</c> for <c>0755</c>. When null, disk adds keep the file mode and other adds leave the mode unset.</summary>
    public int? UnixMode { get; init; }

    /// <summary>Compression, last-write time, and Unix mode. The comment length is shown.</summary>
    public override string ToString() => $"CompressionLevel={CompressionLevel} LastWriteTime={LastWriteTime} CommentLength={Comment?.Length} UnixMode={UnixMode}";
}