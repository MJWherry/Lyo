using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Lyo.Exceptions;

namespace Lyo.Common.Core.Archives;

/// <summary>Fluent builder that packs bytes, streams, text, and disk files into one ZIP archive.</summary>
/// <remarks>
/// Call <see cref="Create" /> or <c>Open</c>, then <c>With*</c> for archive options and <c>Add*</c> or <see cref="Remove" /> for entries. Nothing is written until
/// <see cref="Build" />, <see cref="BuildTo" />, or their async forms. Streams passed to <c>AddFile</c> stay open and are copied at build time. Adds after build throw. A second build
/// returns the same bytes.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ZipFileBuilder
{
    private readonly List<PendingEntry> _entries = [];
    private readonly HashSet<string> _names = new(StringComparer.Ordinal);
    private readonly HashSet<string> _removed = new(StringComparer.Ordinal);
    private string? _comment;
    private string? _sourcePath;
    private Stream? _sourceStream;
    private bool _leaveSourceOpen = true;
    private bool _closed;
    private CompressionLevel _compressionLevel = CompressionLevel.Optimal;
    private byte[]? _built;

    private ZipFileBuilder() { }

    /// <summary>Starts a new builder.</summary>
    /// <returns>An empty builder.</returns>
    public static ZipFileBuilder Create() => new();

    /// <summary>Opens an existing archive for update. Entry bodies are not read.</summary>
    /// <param name="path">Path of the ZIP file.</param>
    /// <returns>A builder whose existing entry names are already taken.</returns>
    public static ZipFileBuilder Open(string path)
    {
        ArgumentHelpers.ThrowIfFileNotFound(path);
        var builder = new ZipFileBuilder { _sourcePath = path };
        using var stream = File.OpenRead(path);
        builder.ReadExisting(stream);
        return builder;
    }

    /// <summary>Opens an existing archive for update. The stream must be seekable. Entry bodies are not read.</summary>
    /// <param name="stream">Archive bytes. Position is restored after the central directory is read.</param>
    /// <param name="leaveOpen">When false, the stream is disposed after the builder is written.</param>
    /// <returns>A builder whose existing entry names are already taken.</returns>
    public static ZipFileBuilder Open(Stream stream, bool leaveOpen = true)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        ArgumentHelpers.ThrowIf(!stream.CanSeek, "ZIP update requires a seekable stream.");
        var builder = new ZipFileBuilder { _sourceStream = stream, _leaveSourceOpen = leaveOpen };
        builder.ReadExisting(stream);
        return builder;
    }

    /// <summary>Drops a queued entry, or marks an existing entry for deletion on the next build.</summary>
    /// <param name="entryName">Entry name. A trailing slash is ignored.</param>
    /// <returns>This builder.</returns>
    public ZipFileBuilder Remove(string entryName)
    {
        EnsureOpen();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entryName);
        var key = EntryKey(entryName);
        var index = _entries.FindIndex(entry => EntryKey(entry.Name) == key);
        if (index >= 0) {
            _entries.RemoveAt(index);
            _names.Remove(key);
            return this;
        }

        ArgumentHelpers.ThrowIf(!_names.Remove(key), $"ZIP entry '{entryName}' was not found.");
        _removed.Add(key);
        return this;
    }

    /// <summary>Sets the compression level used for entries that do not override it. The default is <see cref="CompressionLevel.Optimal" />.</summary>
    /// <param name="level">Level applied at build time.</param>
    /// <returns>This builder.</returns>
    public ZipFileBuilder WithCompressionLevel(CompressionLevel level)
    {
        EnsureOpen();
        _compressionLevel = level;
        return this;
    }

    /// <summary>Sets the ZIP archive comment. Null clears it.</summary>
    /// <param name="comment">Comment stored on the archive.</param>
    /// <returns>This builder.</returns>
    public ZipFileBuilder WithComment(string? comment)
    {
        EnsureOpen();
        _comment = comment;
        return this;
    }

    /// <summary>Queues a file from a byte array.</summary>
    /// <param name="entryName">Relative entry name inside the archive.</param>
    /// <param name="data">File bytes.</param>
    /// <param name="options">Optional compression, last-write time, and comment. Last-write time defaults to UTC now.</param>
    /// <returns>This builder.</returns>
    public ZipFileBuilder AddFile(string entryName, byte[] data, ZipEntryOptions options = default)
    {
        EnsureOpen();
        ArgumentHelpers.ThrowIfNull(data);
        AddPending(NormalizeEntryName(entryName), options, false, data, null, null, options.LastWriteTime ?? DateTimeOffset.UtcNow, options.UnixMode);
        return this;
    }

    /// <summary>Queues a file from a stream. The stream must stay open until build and is not disposed.</summary>
    /// <param name="entryName">Relative entry name inside the archive.</param>
    /// <param name="data">Bytes copied from the stream's current position at build time.</param>
    /// <param name="options">Optional compression, last-write time, and comment. Last-write time defaults to UTC now.</param>
    /// <returns>This builder.</returns>
    public ZipFileBuilder AddFile(string entryName, Stream data, ZipEntryOptions options = default)
    {
        EnsureOpen();
        ArgumentHelpers.ThrowIfNull(data);
        AddPending(NormalizeEntryName(entryName), options, false, null, data, null, options.LastWriteTime ?? DateTimeOffset.UtcNow, options.UnixMode);
        return this;
    }

    /// <summary>Queues a text file.</summary>
    /// <param name="entryName">Relative entry name inside the archive.</param>
    /// <param name="text">Text to encode.</param>
    /// <param name="encoding">Encoding used for the bytes. UTF-8 when omitted.</param>
    /// <param name="options">Optional compression, last-write time, and comment. Last-write time defaults to UTC now.</param>
    /// <returns>This builder.</returns>
    public ZipFileBuilder AddFile(string entryName, string text, Encoding? encoding = null, ZipEntryOptions options = default)
    {
        EnsureOpen();
        ArgumentHelpers.ThrowIfNull(text);
        encoding ??= Encoding.UTF8;
        return AddFile(entryName, encoding.GetBytes(text), options);
    }

    /// <summary>Queues several files from a name-to-bytes map.</summary>
    /// <param name="files">Map of entry name to file bytes.</param>
    /// <returns>This builder.</returns>
    public ZipFileBuilder AddFiles(IReadOnlyDictionary<string, byte[]> files)
    {
        EnsureOpen();
        ArgumentHelpers.ThrowIfNull(files);
        foreach (var file in files) {
            ArgumentHelpers.ThrowIfNull(file.Value);
            AddFile(file.Key, file.Value);
        }

        return this;
    }

    /// <summary>Queues several files from disk. Each entry name is the file name, and the last-write time is taken from the file.</summary>
    /// <param name="filePaths">Paths to include.</param>
    /// <returns>This builder.</returns>
    public ZipFileBuilder AddFiles(params string[] filePaths)
    {
        EnsureOpen();
        ArgumentHelpers.ThrowIfNull(filePaths);
        foreach (var path in filePaths)
            AddFileFromPath(path);

        return this;
    }

    /// <summary>Queues one file from disk. The last-write time and, off Windows, the Unix mode are taken from the file unless <paramref name="options" /> sets them.</summary>
    /// <param name="filePath">Path of the file to add.</param>
    /// <param name="entryName">Archive name. Defaults to the file name.</param>
    /// <param name="options">Optional compression, last-write time, comment, and Unix mode.</param>
    /// <returns>This builder.</returns>
    public ZipFileBuilder AddFileFromPath(string filePath, string? entryName = null, ZipEntryOptions options = default)
    {
        EnsureOpen();
        ArgumentHelpers.ThrowIfFileNotFound(filePath);
        entryName ??= Path.GetFileName(filePath);
        var written = options.LastWriteTime ?? new DateTimeOffset(File.GetLastWriteTime(filePath));
        AddPending(NormalizeEntryName(entryName), options, false, null, null, filePath, written, options.UnixMode ?? UnixModeFromDisk(filePath));
        return this;
    }

    /// <summary>Queues every file under a directory, plus an entry for each empty subdirectory.</summary>
    /// <param name="directoryPath">Directory to include.</param>
    /// <param name="entryPrefix">Prefix prepended to each entry name. Empty leaves paths relative to <paramref name="directoryPath" />.</param>
    /// <param name="include">When set, a file is skipped if this returns false for its relative path (<c>/</c> separators, no leading slash). Empty directories are still included.</param>
    /// <param name="options">Optional compression, comment, and Unix mode applied to every queued entry. Last-write time and Unix mode fall back to the disk file.</param>
    /// <returns>This builder.</returns>
    public ZipFileBuilder AddDirectory(string directoryPath, string entryPrefix = "", Func<string, bool>? include = null, ZipEntryOptions options = default)
    {
        EnsureOpen();
        ExceptionThrower.ThrowIfDirectoryNotFound(directoryPath);
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)) {
            var relative = ToZipRelative(directoryPath, file);
            if (include is not null && !include(relative))
                continue;

            var written = options.LastWriteTime ?? new DateTimeOffset(File.GetLastWriteTime(file));
            AddPending(NormalizeEntryName(JoinPrefix(entryPrefix, relative)), options, false, null, null, file, written, options.UnixMode ?? UnixModeFromDisk(file));
        }

        foreach (var directory in Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories)) {
            if (Directory.EnumerateFileSystemEntries(directory).Any())
                continue;

            var relative = ToZipRelative(directoryPath, directory);
            var written = options.LastWriteTime ?? new DateTimeOffset(Directory.GetLastWriteTime(directory));
            AddPending(NormalizeEntryName(JoinPrefix(entryPrefix, relative)) + "/", options, true, null, null, null, written, options.UnixMode ?? UnixModeFromDisk(directory));
        }

        return this;
    }

    /// <summary>Writes the archive and returns its bytes.</summary>
    /// <returns>ZIP file bytes. A later call returns the same array.</returns>
    public byte[] Build()
    {
        if (_built is not null)
            return _built;

        EnsureOpen();
        using var memory = new MemoryStream();
        WriteTo(memory);
        _built = memory.ToArray();
        Close();
        return _built;
    }

    /// <summary>Writes the archive into <paramref name="destination" /> without holding the entry bytes in a second buffer.</summary>
    /// <param name="destination">Stream that receives the ZIP. An update needs a seekable stream.</param>
    public void BuildTo(Stream destination)
    {
        ArgumentHelpers.ThrowIfNull(destination);
        if (_built is not null) {
            destination.Write(_built, 0, _built.Length);
            return;
        }

        EnsureOpen();
        WriteTo(destination);
        Close();
    }

    /// <summary>Writes the archive to disk.</summary>
    /// <param name="outputPath">Destination path for the ZIP.</param>
    public void BuildToFile(string outputPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        if (_built is not null) {
            File.WriteAllBytes(outputPath, _built);
            return;
        }

        EnsureOpen();
        if (IsSameSourceFile(outputPath)) {
            using var stream = new FileStream(outputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            UpdateInPlace(stream);
        }
        else {
            using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920);
            WriteTo(stream);
        }

        Close();
    }

    /// <summary>Writes the archive and returns it as a readable stream.</summary>
    /// <returns>A memory stream positioned at the start of the ZIP bytes.</returns>
    public Stream BuildToStream()
    {
        var data = Build();
        return new MemoryStream(data, false);
    }

    /// <summary>Writes the archive and returns its bytes, copying streams and files asynchronously.</summary>
    /// <param name="ct">Cancels the copy.</param>
    /// <returns>ZIP file bytes. A later call returns the same array.</returns>
    public async Task<byte[]> BuildAsync(CancellationToken ct = default)
    {
        if (_built is not null)
            return _built;

        EnsureOpen();
        ct.ThrowIfCancellationRequested();
        using var memory = new MemoryStream();
        await WriteToAsync(memory, ct).ConfigureAwait(false);
        _built = memory.ToArray();
        Close();
        return _built;
    }

    /// <summary>Writes the archive into <paramref name="destination" />, copying streams and files asynchronously.</summary>
    /// <param name="destination">Stream that receives the ZIP. An update needs a seekable stream.</param>
    /// <param name="ct">Cancels the copy.</param>
    public async Task BuildToAsync(Stream destination, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(destination);
        if (_built is not null) {
            await destination.WriteAsync(_built, 0, _built.Length, ct).ConfigureAwait(false);
            return;
        }

        EnsureOpen();
        await WriteToAsync(destination, ct).ConfigureAwait(false);
        Close();
    }

    /// <summary>Writes the archive to disk, copying streams and files asynchronously.</summary>
    /// <param name="outputPath">Destination path for the ZIP.</param>
    /// <param name="ct">Cancels the copy and the file write.</param>
    public async Task BuildToFileAsync(string outputPath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        if (_built is not null) {
#if NETSTANDARD2_0
            using var written = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
            await written.WriteAsync(_built, 0, _built.Length, ct).ConfigureAwait(false);
#else
            await File.WriteAllBytesAsync(outputPath, _built, ct).ConfigureAwait(false);
#endif
            return;
        }

        EnsureOpen();
        if (IsSameSourceFile(outputPath)) {
            using var stream = new FileStream(outputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous);
            await UpdateInPlaceAsync(stream, ct).ConfigureAwait(false);
        }
        else {
            using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous);
            await WriteToAsync(stream, ct).ConfigureAwait(false);
        }

        Close();
    }

    /// <summary>Diagnostic snapshot of queued entries and whether the archive has been built.</summary>
    /// <returns>Text covering built state and entry count.</returns>
    public override string ToString() => $"ZipFileBuilder: Built={_built is not null}, Entries={_entries.Count}";

    private void AddPending(string name, ZipEntryOptions options, bool isDirectory, byte[]? bytes, Stream? source, string? filePath, DateTimeOffset lastWriteTime, int? unixMode)
    {
        var key = name.TrimEnd('/');
        ArgumentHelpers.ThrowIf(!_names.Add(key), $"ZIP entry '{name}' is already present.");
        _entries.Add(new(name, options.CompressionLevel, lastWriteTime, options.Comment, unixMode, isDirectory, bytes, source, filePath));
    }

    private void EnsureOpen() => OperationHelpers.ThrowIf(_closed || _built is not null, "ZipFileBuilder has already been built. Create a new instance to build another zip file.");

    private void Close()
    {
        _closed = true;
        if (_sourceStream is not null && !_leaveSourceOpen)
            _sourceStream.Dispose();

        _sourceStream = null;
    }

    private bool HasSource => _sourcePath is not null || _sourceStream is not null;

    private void ReadExisting(Stream stream)
    {
        var position = stream.Position;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, true);
#if !NETSTANDARD2_0
        if (!string.IsNullOrEmpty(archive.Comment))
            _comment = archive.Comment;
#endif
        foreach (var entry in archive.Entries)
            _names.Add(EntryKey(entry.FullName));

        stream.Position = position;
    }

    private void WriteTo(Stream destination)
    {
        if (!HasSource) {
            CreateTo(destination);
            return;
        }

        if (_sourceStream is null || !ReferenceEquals(destination, _sourceStream)) {
            CopySource(destination);
            ArgumentHelpers.ThrowIf(!destination.CanSeek, "ZIP update requires a seekable destination.");
            destination.Position = 0;
        }

        UpdateInPlace(destination);
    }

    private async Task WriteToAsync(Stream destination, CancellationToken ct)
    {
        if (!HasSource) {
            await CreateToAsync(destination, ct).ConfigureAwait(false);
            return;
        }

        if (_sourceStream is null || !ReferenceEquals(destination, _sourceStream)) {
            await CopySourceAsync(destination, ct).ConfigureAwait(false);
            ArgumentHelpers.ThrowIf(!destination.CanSeek, "ZIP update requires a seekable destination.");
            destination.Position = 0;
        }

        await UpdateInPlaceAsync(destination, ct).ConfigureAwait(false);
    }

    private void CreateTo(Stream destination)
    {
        var target = BeginCreate(destination, out var buffer);
        using (var archive = new ZipArchive(target, ZipArchiveMode.Create, true)) {
            SetArchiveComment(archive);
            foreach (var pending in _entries)
                WritePending(archive, pending);
        }

        FinishCreate(destination, target, buffer);
    }

    private async Task CreateToAsync(Stream destination, CancellationToken ct)
    {
        var target = BeginCreate(destination, out var buffer);
        using (var archive = new ZipArchive(target, ZipArchiveMode.Create, true)) {
            SetArchiveComment(archive);
            foreach (var pending in _entries)
                await WritePendingAsync(archive, pending, ct).ConfigureAwait(false);
        }

        FinishCreate(destination, target, buffer);
    }

    private Stream BeginCreate(Stream destination, out MemoryStream? buffer)
    {
        buffer = null;
        if (!NeedsByteCommentPatch || destination.CanSeek)
            return destination;

        buffer = new();
        return buffer;
    }

    private void FinishCreate(Stream destination, Stream target, MemoryStream? buffer)
    {
        if (buffer is not null) {
            var patched = ApplyComments(buffer.ToArray());
            destination.Write(patched, 0, patched.Length);
            return;
        }

        if (NeedsByteCommentPatch)
            PatchSeekable(target);
    }

    private void UpdateInPlace(Stream destination)
    {
        using var archive = new ZipArchive(destination, ZipArchiveMode.Update, true);
        SetArchiveComment(archive);
        DeleteRemoved(archive);
        foreach (var pending in _entries)
            WritePending(archive, pending);
    }

    private async Task UpdateInPlaceAsync(Stream destination, CancellationToken ct)
    {
        using var archive = new ZipArchive(destination, ZipArchiveMode.Update, true);
        SetArchiveComment(archive);
        DeleteRemoved(archive);
        foreach (var pending in _entries)
            await WritePendingAsync(archive, pending, ct).ConfigureAwait(false);
    }

    private void DeleteRemoved(ZipArchive archive)
    {
        foreach (var entry in archive.Entries.ToArray()) {
            if (_removed.Contains(EntryKey(entry.FullName)))
                entry.Delete();
        }
    }

    private void CopySource(Stream destination)
    {
        if (_sourcePath is not null) {
            using var input = File.OpenRead(_sourcePath);
            input.CopyTo(destination);
            return;
        }

        var stream = _sourceStream;
        ArgumentHelpers.ThrowIfNull(stream);
        var position = stream.Position;
        stream.Position = 0;
        stream.CopyTo(destination);
        stream.Position = position;
    }

    private async Task CopySourceAsync(Stream destination, CancellationToken ct)
    {
        if (_sourcePath is not null) {
            using var input = new FileStream(_sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
#if NETSTANDARD2_0
            await input.CopyToAsync(destination, 81920, ct).ConfigureAwait(false);
#else
            await input.CopyToAsync(destination, ct).ConfigureAwait(false);
#endif
            return;
        }

        var stream = _sourceStream;
        ArgumentHelpers.ThrowIfNull(stream);
        var position = stream.Position;
        stream.Position = 0;
#if NETSTANDARD2_0
        await stream.CopyToAsync(destination, 81920, ct).ConfigureAwait(false);
#else
        await stream.CopyToAsync(destination, ct).ConfigureAwait(false);
#endif
        stream.Position = position;
    }

    private void PatchSeekable(Stream stream)
    {
        stream.Position = 0;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        var patched = ApplyComments(copy.ToArray());
        stream.Position = 0;
        stream.SetLength(0);
        stream.Write(patched, 0, patched.Length);
    }

    private void SetArchiveComment(ZipArchive archive)
    {
#if !NETSTANDARD2_0
        if (_comment is not null)
            archive.Comment = _comment;
#endif
    }

    private bool IsSameSourceFile(string outputPath)
    {
        if (_sourcePath is null)
            return false;

        var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(Path.GetFullPath(outputPath), Path.GetFullPath(_sourcePath), comparison);
    }

    private static int? UnixModeFromDisk(string path)
    {
#if NET7_0_OR_GREATER
        if (OperatingSystem.IsWindows())
            return null;

        return (int)File.GetUnixFileMode(path);
#else
        return null;
#endif
    }

#if NETSTANDARD2_0
    private bool NeedsByteCommentPatch => _comment is not null || _entries.Exists(entry => entry.Comment is not null || entry.UnixMode is not null);
#else
    private bool NeedsByteCommentPatch => false;
#endif

    private void WritePending(ZipArchive archive, PendingEntry pending)
    {
        var entry = archive.CreateEntry(pending.Name, pending.Level ?? _compressionLevel);
        Apply(entry, pending);
        if (pending.IsDirectory)
            return;

        using var destination = entry.Open();
        Copy(pending, destination);
    }

    private async Task WritePendingAsync(ZipArchive archive, PendingEntry pending, CancellationToken ct)
    {
        var entry = archive.CreateEntry(pending.Name, pending.Level ?? _compressionLevel);
        Apply(entry, pending);
        if (pending.IsDirectory)
            return;

        using var destination = entry.Open();
        await CopyAsync(pending, destination, ct).ConfigureAwait(false);
    }

    private static void Apply(ZipArchiveEntry entry, PendingEntry pending)
    {
        entry.LastWriteTime = pending.LastWriteTime;
#if !NETSTANDARD2_0
        if (pending.UnixMode is int mode)
            entry.ExternalAttributes = (mode << 16) | (pending.IsDirectory ? 0x10 : 0);
        if (pending.Comment is not null)
            entry.Comment = pending.Comment;
#endif
    }

    // ZipArchive.Comment and ZipArchiveEntry.Comment are absent on netstandard2.0, so comments are patched into the finished archive.
    private byte[] ApplyComments(byte[] zip)
    {
        zip = ApplyEntryComments(zip);
#if NETSTANDARD2_0
        zip = ApplyUnixModes(zip);
#endif
        if (_comment is not null)
            zip = ApplyArchiveComment(zip, _comment);

        return zip;
    }

    private byte[] ApplyEntryComments(byte[] zip)
    {
        Dictionary<string, string>? comments = null;
        foreach (var pending in _entries) {
            if (pending.Comment is null)
                continue;

            comments ??= new(StringComparer.Ordinal);
            comments[pending.Name] = pending.Comment;
        }

        if (comments is null)
            return zip;

        var eocd = FindEocd(zip);
        var location = LocateCentralDirectory(zip, eocd);
        OperationHelpers.ThrowIf(location.Offset < 0 || location.Offset + location.Size > zip.Length, "ZIP central directory is outside the archive.");
        var cdStart = (int)location.Offset;
        var cursor = cdStart;
        var cdEnd = cdStart + (int)location.Size;
        using var rebuilt = new MemoryStream();
        while (cursor < cdEnd) {
            OperationHelpers.ThrowIf(ReadInt32(zip, cursor) != 0x02014b50, "ZIP central directory entry was not found.");
            var nameLength = ReadUInt16(zip, cursor + 28);
            var extraLength = ReadUInt16(zip, cursor + 30);
            var commentLength = ReadUInt16(zip, cursor + 32);
            var headerLength = 46 + nameLength + extraLength;
            var name = Encoding.UTF8.GetString(zip, cursor + 46, nameLength);
            var header = new byte[headerLength];
            Buffer.BlockCopy(zip, cursor, header, 0, headerLength);
            byte[] commentBytes;
            if (comments.TryGetValue(name, out var comment)) {
                commentBytes = Encoding.UTF8.GetBytes(comment);
                ArgumentHelpers.ThrowIf(commentBytes.Length > ushort.MaxValue, "ZIP entry comment exceeds 65535 bytes.");
                WriteUInt16(header, 32, (ushort)commentBytes.Length);
            }
            else {
                commentBytes = new byte[commentLength];
                Buffer.BlockCopy(zip, cursor + headerLength, commentBytes, 0, commentLength);
            }

            rebuilt.Write(header, 0, header.Length);
            rebuilt.Write(commentBytes, 0, commentBytes.Length);
            cursor += headerLength + commentLength;
        }

        var centralDirectory = rebuilt.ToArray();
        var tailLength = zip.Length - cdEnd;
        var result = new byte[cdStart + centralDirectory.Length + tailLength];
        Buffer.BlockCopy(zip, 0, result, 0, cdStart);
        Buffer.BlockCopy(centralDirectory, 0, result, cdStart, centralDirectory.Length);
        Buffer.BlockCopy(zip, cdEnd, result, cdStart + centralDirectory.Length, tailLength);
        var delta = centralDirectory.Length - location.Size;
        if (delta != 0)
            WriteCentralDirectorySize(result, location, delta);

        return result;
    }

#if NETSTANDARD2_0
    private byte[] ApplyUnixModes(byte[] zip)
    {
        Dictionary<string, int>? modes = null;
        foreach (var pending in _entries) {
            if (pending.UnixMode is not int mode)
                continue;

            modes ??= new(StringComparer.Ordinal);
            modes[pending.Name] = (mode << 16) | (pending.IsDirectory ? 0x10 : 0);
        }

        if (modes is null)
            return zip;

        var location = LocateCentralDirectory(zip, FindEocd(zip));
        var cursor = (int)location.Offset;
        var cdEnd = cursor + (int)location.Size;
        while (cursor < cdEnd) {
            var nameLength = ReadUInt16(zip, cursor + 28);
            var extraLength = ReadUInt16(zip, cursor + 30);
            var commentLength = ReadUInt16(zip, cursor + 32);
            var name = Encoding.UTF8.GetString(zip, cursor + 46, nameLength);
            if (modes.TryGetValue(name, out var attributes))
                WriteInt32(zip, cursor + 38, attributes);

            cursor += 46 + nameLength + extraLength + commentLength;
        }

        return zip;
    }
#endif

    private static byte[] ApplyArchiveComment(byte[] zip, string comment)
    {
        var commentBytes = Encoding.UTF8.GetBytes(comment);
        ArgumentHelpers.ThrowIf(commentBytes.Length > ushort.MaxValue, "ZIP archive comment exceeds 65535 bytes.");
        var eocd = FindEocd(zip);
        var commentStart = eocd + 22;
        var result = new byte[commentStart + commentBytes.Length];
        Buffer.BlockCopy(zip, 0, result, 0, commentStart);
        WriteUInt16(result, eocd + 20, (ushort)commentBytes.Length);
        Buffer.BlockCopy(commentBytes, 0, result, commentStart, commentBytes.Length);
        return result;
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

    private readonly record struct CentralDirectoryLocation(long Offset, long Size, bool Zip64);

    private static CentralDirectoryLocation LocateCentralDirectory(byte[] zip, int eocd)
    {
        var size = ReadInt32(zip, eocd + 12);
        var offset = ReadInt32(zip, eocd + 16);
        if (size != -1 && offset != -1)
            return new(offset, size, false);

        var locator = eocd - 20;
        OperationHelpers.ThrowIf(locator < 0 || ReadInt32(zip, locator) != 0x07064b50, "ZIP64 end of central directory locator was not found.");
        var zip64Offset = ReadInt64(zip, locator + 8);
        OperationHelpers.ThrowIf(zip64Offset < 0 || zip64Offset > int.MaxValue, "ZIP64 end of central directory is outside the archive.");
        var zip64 = (int)zip64Offset;
        OperationHelpers.ThrowIf(ReadInt32(zip, zip64) != 0x06064b50, "ZIP64 end of central directory was not found.");
        return new(ReadInt64(zip, zip64 + 48), ReadInt64(zip, zip64 + 40), true);
    }

    private static void WriteCentralDirectorySize(byte[] zip, CentralDirectoryLocation location, long delta)
    {
        var eocd = FindEocd(zip);
        if (!location.Zip64) {
            WriteInt32(zip, eocd + 12, checked((int)(location.Size + delta)));
            return;
        }

        var locator = eocd - 20;
        var zip64Offset = ReadInt64(zip, locator + 8) + delta;
        WriteInt64(zip, locator + 8, zip64Offset);
        OperationHelpers.ThrowIf(zip64Offset < 0 || zip64Offset > int.MaxValue, "ZIP64 end of central directory is outside the archive.");
        WriteInt64(zip, (int)zip64Offset + 40, location.Size + delta);
    }

    private static ushort ReadUInt16(byte[] buffer, int offset) => (ushort)(buffer[offset] | (buffer[offset + 1] << 8));

    private static int ReadInt32(byte[] buffer, int offset) => buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24);

    private static long ReadInt64(byte[] buffer, int offset)
    {
        var low = (uint)ReadInt32(buffer, offset);
        var high = (uint)ReadInt32(buffer, offset + 4);
        return low | ((long)high << 32);
    }

    private static void WriteInt64(byte[] buffer, int offset, long value)
    {
        WriteInt32(buffer, offset, (int)value);
        WriteInt32(buffer, offset + 4, (int)(value >> 32));
    }

    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }

    private static void Copy(PendingEntry pending, Stream destination)
    {
        if (pending.Bytes is not null) {
            destination.Write(pending.Bytes, 0, pending.Bytes.Length);
            return;
        }

        if (pending.Source is not null) {
            pending.Source.CopyTo(destination);
            return;
        }

        var filePath = pending.FilePath;
        ArgumentHelpers.ThrowIfNull(filePath);
        using var input = File.OpenRead(filePath);
        input.CopyTo(destination);
    }

    private static async Task CopyAsync(PendingEntry pending, Stream destination, CancellationToken ct)
    {
        if (pending.Bytes is not null) {
            await destination.WriteAsync(pending.Bytes, 0, pending.Bytes.Length, ct).ConfigureAwait(false);
            return;
        }

        if (pending.Source is not null) {
#if NETSTANDARD2_0
            await pending.Source.CopyToAsync(destination, 81920, ct).ConfigureAwait(false);
#else
            await pending.Source.CopyToAsync(destination, ct).ConfigureAwait(false);
#endif
            return;
        }

        var filePath = pending.FilePath;
        ArgumentHelpers.ThrowIfNull(filePath);
        using var input = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
#if NETSTANDARD2_0
        await input.CopyToAsync(destination, 81920, ct).ConfigureAwait(false);
#else
        await input.CopyToAsync(destination, ct).ConfigureAwait(false);
#endif
    }

    private static string EntryKey(string name) => name.Replace('\\', '/').TrimEnd('/');

    private static string JoinPrefix(string entryPrefix, string relative)
    {
        if (string.IsNullOrEmpty(entryPrefix))
            return relative;

        var prefix = entryPrefix.Replace('\\', '/').Trim('/');
        if (prefix.Length == 0)
            return relative;

        return prefix + "/" + relative.TrimStart('/');
    }

    private static string ToZipRelative(string directoryPath, string path) => GetRelativePathCompat(directoryPath, path).Replace('\\', '/');

    private static string NormalizeEntryName(string entryName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entryName);
        var normalized = entryName.Replace('\\', '/');
        var rooted = normalized[0] == '/' || HasDrivePrefix(normalized) || Path.IsPathRooted(entryName);
        var parent = false;
        foreach (var segment in normalized.Split('/')) {
            if (segment == "..") {
                parent = true;
                break;
            }
        }

        FormatHelpers.ThrowIf(rooted || parent, "ZIP entry name must be a relative path without a '..' segment.", nameof(entryName), entryName, "relative/path/file.txt");
        var trimmed = normalized.Trim('/');
        ArgumentHelpers.ThrowIf(trimmed.Length == 0, "ZIP entry name is empty.", nameof(entryName));
        return trimmed;
    }

    private static bool HasDrivePrefix(string normalized) => normalized.Length >= 2 && normalized[1] == ':';

    private static string GetRelativePathCompat(string basePath, string path)
    {
#if NETSTANDARD2_0
        return GetRelativePathNetStandard(basePath, path);
#else
        return Path.GetRelativePath(basePath, path);
#endif
    }

#if NETSTANDARD2_0
    private static string GetRelativePathNetStandard(string basePath, string targetPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(basePath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(targetPath);
        var baseUri = new Uri(AppendSlash(basePath));
        var targetUri = new Uri(targetPath);
        var relativeUri = baseUri.MakeRelativeUri(targetUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendSlash(string path) => !path.EndsWith(Path.DirectorySeparatorChar.ToString()) ? path + Path.DirectorySeparatorChar : path;
#endif

    private sealed class PendingEntry(
        string name,
        CompressionLevel? level,
        DateTimeOffset lastWriteTime,
        string? comment,
        int? unixMode,
        bool isDirectory,
        byte[]? bytes,
        Stream? source,
        string? filePath)
    {
        public string Name { get; } = name;

        public CompressionLevel? Level { get; } = level;

        public DateTimeOffset LastWriteTime { get; } = lastWriteTime;

        public string? Comment { get; } = comment;

        public int? UnixMode { get; } = unixMode;

        public bool IsDirectory { get; } = isDirectory;

        public byte[]? Bytes { get; } = bytes;

        public Stream? Source { get; } = source;

        public string? FilePath { get; } = filePath;
    }
}