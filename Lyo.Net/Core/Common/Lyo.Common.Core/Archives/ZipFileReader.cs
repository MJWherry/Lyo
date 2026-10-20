using System.IO.Compression;
using System.Text;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;

namespace Lyo.Common.Core.Archives;

/// <summary>Reads and extracts a ZIP archive.</summary>
/// <remarks>
/// <see cref="OpenEntry" /> returns the archive's entry stream. Dispose that stream before opening another entry. <see cref="ExtractTo" /> refuses an entry whose path escapes the destination directory.
/// </remarks>
public sealed class ZipFileReader : IDisposable
{
    private readonly ZipArchive _archive;

    private ZipFileReader(ZipArchive archive, Stream stream)
    {
        _archive = archive;
        var entries = archive.Entries.Select(Describe).ToArray();
#if NETSTANDARD2_0
        Comment = ApplyStoredMetadata(stream, entries);
#else
        Comment = string.IsNullOrEmpty(archive.Comment) ? null : archive.Comment;
#endif
        Entries = entries;
    }

    /// <summary>Archive comment. Null when the archive has none.</summary>
    public string? Comment { get; }

    /// <summary>Entries in archive order.</summary>
    public IReadOnlyList<ZipFileEntry> Entries { get; }

    /// <summary>Opens a ZIP file for reading.</summary>
    /// <param name="path">Path of the ZIP file.</param>
    /// <returns>A reader that owns the file stream.</returns>
    public static ZipFileReader Open(string path)
    {
        ArgumentHelpers.ThrowIfFileNotFound(path);
        var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Open(stream, leaveOpen: false);
    }

    /// <summary>Opens a ZIP stream for reading.</summary>
    /// <param name="stream">Archive bytes. The stream must stay open until the reader is disposed.</param>
    /// <param name="leaveOpen">When false, disposing the reader disposes <paramref name="stream" />.</param>
    /// <returns>A reader over the stream.</returns>
    public static ZipFileReader Open(Stream stream, bool leaveOpen = true)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        return new(new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen), stream);
    }

    /// <summary>Reads one entry into memory.</summary>
    /// <param name="entryName">Entry name stored in the archive.</param>
    /// <returns>Uncompressed entry bytes.</returns>
    public byte[] Read(string entryName)
    {
        using var stream = OpenEntry(entryName);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>Opens one entry. Dispose the returned stream before opening another entry.</summary>
    /// <param name="entryName">Entry name stored in the archive.</param>
    /// <returns>Uncompressed entry bytes.</returns>
    public Stream OpenEntry(string entryName) => Require(entryName).Open();

    /// <summary>Extracts every entry under <paramref name="directoryPath" />, restoring last-write time and Unix mode.</summary>
    /// <param name="directoryPath">Destination directory. It is created when missing.</param>
    public void ExtractTo(string directoryPath)
    {
        var root = PrepareDestination(directoryPath);
        foreach (var entry in _archive.Entries) {
            var target = TargetPath(root, entry.FullName);
            if (IsDirectory(entry)) {
                Directory.CreateDirectory(target);
                Restore(target, entry, directory: true);
                continue;
            }

            var parent = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            using (var input = entry.Open())
            using (var output = File.Create(target))
                input.CopyTo(output);

            Restore(target, entry, directory: false);
        }
    }

    /// <summary>Extracts every entry under <paramref name="directoryPath" />, copying file bytes asynchronously.</summary>
    /// <param name="directoryPath">Destination directory. It is created when missing.</param>
    /// <param name="ct">Cancels the copy.</param>
    public async Task ExtractToAsync(string directoryPath, CancellationToken ct = default)
    {
        var root = PrepareDestination(directoryPath);
        foreach (var entry in _archive.Entries) {
            ct.ThrowIfCancellationRequested();
            var target = TargetPath(root, entry.FullName);
            if (IsDirectory(entry)) {
                Directory.CreateDirectory(target);
                Restore(target, entry, directory: true);
                continue;
            }

            var parent = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            using (var input = entry.Open())
            using (var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous)) {
#if NETSTANDARD2_0
                await input.CopyToAsync(output, 81920, ct).ConfigureAwait(false);
#else
                await input.CopyToAsync(output, ct).ConfigureAwait(false);
#endif
            }

            Restore(target, entry, directory: false);
        }
    }

    /// <summary>Closes the archive.</summary>
    public void Dispose() => _archive.Dispose();

    private ZipArchiveEntry Require(string entryName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entryName);
        var entry = _archive.GetEntry(entryName);
        ArgumentHelpers.ThrowIf(entry is null, $"ZIP entry '{entryName}' was not found.");
        return entry!;
    }

    private static string PrepareDestination(string directoryPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(directoryPath);
        Directory.CreateDirectory(directoryPath);
        return Path.GetFullPath(directoryPath);
    }

    private static string TargetPath(string root, string entryName)
    {
        var relative = entryName.Replace('\\', '/').TrimStart('/');
        var combined = Path.GetFullPath(Path.Combine(root, relative));
        PathHelpers.ThrowIfEscapesRoot(PathStyle.Host, root, combined);
        return combined;
    }

    private static void Restore(string path, ZipArchiveEntry entry, bool directory)
    {
        if (directory)
            Directory.SetLastWriteTime(path, entry.LastWriteTime.DateTime);
        else
            File.SetLastWriteTime(path, entry.LastWriteTime.DateTime);

        var mode = UnixMode(entry);
#if NET7_0_OR_GREATER
        if (mode is not null && !OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, (UnixFileMode)mode.Value);
#endif
    }

    private static ZipFileEntry Describe(ZipArchiveEntry entry)
    {
        string? comment = null;
#if !NETSTANDARD2_0
        if (!string.IsNullOrEmpty(entry.Comment))
            comment = entry.Comment;
#endif
        return new() {
            Name = entry.FullName,
            Length = entry.Length,
            CompressedLength = entry.CompressedLength,
            LastWriteTime = entry.LastWriteTime,
            Comment = comment,
            UnixMode = UnixMode(entry),
            IsDirectory = IsDirectory(entry)
        };
    }

    private static int? UnixMode(ZipArchiveEntry entry)
    {
#if NETSTANDARD2_0
        return null;
#else
        var mode = entry.ExternalAttributes >> 16;
        return mode == 0 ? null : mode;
#endif
    }

    private static bool IsDirectory(ZipArchiveEntry entry)
    {
        var name = entry.FullName;
        return name.EndsWith("/", StringComparison.Ordinal) || name.EndsWith("\\", StringComparison.Ordinal);
    }

#if NETSTANDARD2_0
    private static string? ApplyStoredMetadata(Stream stream, ZipFileEntry[] entries)
    {
        if (!stream.CanSeek)
            return null;

        var length = stream.Length;
        var position = stream.Position;
        var tailCount = (int)Math.Min(length, 22 + ushort.MaxValue);
        var tail = new byte[tailCount];
        stream.Position = length - tailCount;
        ReadExactly(stream, tail, 0, tailCount);
        var eocdInTail = FindEocd(tail);
        var commentLength = ReadUInt16(tail, eocdInTail + 20);
        string? comment = commentLength == 0 ? null : Encoding.UTF8.GetString(tail, eocdInTail + 22, commentLength);
        var cd = ReadCentralDirectory(stream, tail, eocdInTail);
        var cursor = 0;
        var index = 0;
        while (cursor < cd.Length && index < entries.Length) {
            var nameLength = ReadUInt16(cd, cursor + 28);
            var extraLength = ReadUInt16(cd, cursor + 30);
            var entryCommentLength = ReadUInt16(cd, cursor + 32);
            var headerLength = 46 + nameLength + extraLength;
            var mode = ReadInt32(cd, cursor + 38) >> 16;
            var entryComment = entryCommentLength == 0 ? null : Encoding.UTF8.GetString(cd, cursor + headerLength, entryCommentLength);
            entries[index] = entries[index] with {
                Comment = string.IsNullOrEmpty(entryComment) ? null : entryComment,
                UnixMode = mode == 0 ? null : mode
            };
            cursor += headerLength + entryCommentLength;
            index++;
        }

        stream.Position = position;
        return string.IsNullOrEmpty(comment) ? null : comment;
    }

    private static byte[] ReadCentralDirectory(Stream stream, byte[] tail, int eocdInTail)
    {
        var size = ReadInt32(tail, eocdInTail + 12);
        var offset = ReadInt32(tail, eocdInTail + 16);
        if (size == -1 || offset == -1) {
            var locator = eocdInTail - 20;
            OperationHelpers.ThrowIf(locator < 0 || ReadInt32(tail, locator) != 0x07064b50, "ZIP64 end of central directory locator was not found.");
            var zip64Offset = ReadInt64(tail, locator + 8);
            OperationHelpers.ThrowIf(zip64Offset < 0 || zip64Offset > stream.Length - 56, "ZIP64 end of central directory is outside the archive.");
            var zip64 = new byte[56];
            stream.Position = zip64Offset;
            ReadExactly(stream, zip64, 0, zip64.Length);
            OperationHelpers.ThrowIf(ReadInt32(zip64, 0) != 0x06064b50, "ZIP64 end of central directory was not found.");
            offset = ReadInt32(zip64, 48);
            size = ReadInt32(zip64, 40);
            var offsetHigh = ReadInt32(zip64, 52);
            var sizeHigh = ReadInt32(zip64, 44);
            OperationHelpers.ThrowIf(offsetHigh != 0 || sizeHigh != 0 || offset < 0 || size < 0, "ZIP central directory is outside the archive.");
        }

        OperationHelpers.ThrowIf(offset < 0 || size < 0 || offset + (long)size > stream.Length, "ZIP central directory is outside the archive.");
        var directory = new byte[size];
        stream.Position = offset;
        ReadExactly(stream, directory, 0, directory.Length);
        return directory;
    }

    private static int FindEocd(byte[] zip)
    {
        var min = Math.Max(0, zip.Length - 22 - ushort.MaxValue);
        for (var i = zip.Length - 22; i >= min; i--) {
            if (zip[i] != 0x50 || zip[i + 1] != 0x4b || zip[i + 2] != 0x05 || zip[i + 3] != 0x06)
                continue;

            if (i + 22 + ReadUInt16(zip, i + 20) == zip.Length)
                return i;
        }

        OperationHelpers.ThrowIf(true, "ZIP end of central directory was not found.");
        return 0;
    }

    private static void ReadExactly(Stream stream, byte[] buffer, int offset, int count)
    {
        while (count > 0) {
            var read = stream.Read(buffer, offset, count);
            OperationHelpers.ThrowIf(read == 0, "ZIP archive ended before the central directory.");
            offset += read;
            count -= read;
        }
    }

    private static ushort ReadUInt16(byte[] buffer, int offset) => (ushort)(buffer[offset] | (buffer[offset + 1] << 8));

    private static int ReadInt32(byte[] buffer, int offset)
        => buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24);

    private static long ReadInt64(byte[] buffer, int offset)
    {
        var low = (uint)ReadInt32(buffer, offset);
        var high = (uint)ReadInt32(buffer, offset + 4);
        return (long)low | ((long)high << 32);
    }
#endif
}
