using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Core.Pathing;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.IO.Temp.Enums;
using Microsoft.Extensions.Logging;

namespace Lyo.IO.Temp.Models;

/// <inheritdoc />
// ReSharper disable once InconsistentNaming
public sealed class IOTempFileGenerator : IIOTempFileGenerator
{
    private const int RandomChunkSize = 81920; // 80 KB write buffer

    private static readonly string[] WordBank = [
        "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog", "hello", "world",
        "temp", "file", "data", "test", "sample", "value", "alpha", "beta", "gamma", "delta",
        "lorem", "ipsum", "dolor", "amet", "red", "green", "blue", "black", "white", "fast",
        "slow", "big", "small"
    ];

    private static readonly ArtifactMetrics RandomFileMetrics = new(
        "random file", Constants.Metrics.CreateRandomFileDuration, Constants.Metrics.CreateRandomFileSuccess, Constants.Metrics.CreateRandomFileFailure);

    private static readonly ArtifactMetrics TextFileMetrics = new(
        "text file", Constants.Metrics.CreateTextFileDuration, Constants.Metrics.CreateTextFileSuccess, Constants.Metrics.CreateTextFileFailure);

    private static readonly ArtifactMetrics CsvFileMetrics = new(
        "CSV file", Constants.Metrics.CreateCsvFileDuration, Constants.Metrics.CreateCsvFileSuccess, Constants.Metrics.CreateCsvFileFailure);

    private static readonly ArtifactMetrics JsonFileMetrics = new(
        "JSON file", Constants.Metrics.CreateJsonFileDuration, Constants.Metrics.CreateJsonFileSuccess, Constants.Metrics.CreateJsonFileFailure);

    private static readonly ArtifactMetrics XmlFileMetrics = new(
        "XML file", Constants.Metrics.CreateXmlFileDuration, Constants.Metrics.CreateXmlFileSuccess, Constants.Metrics.CreateXmlFileFailure);

    private static readonly ArtifactMetrics SimulatedDirectoryMetrics = new(
        "simulated directory", Constants.Metrics.SimulateDirectoryDuration, Constants.Metrics.SimulateDirectorySuccess, Constants.Metrics.SimulateDirectoryFailure);

    private static readonly ArtifactMetrics DirectoryTreeMetrics = new(
        "directory tree", Constants.Metrics.CreateDirectoryTreeDuration, Constants.Metrics.CreateDirectoryTreeSuccess, Constants.Metrics.CreateDirectoryTreeFailure);

    private static long _nameSequence;

    [ThreadStatic]
    private static Random? _threadRandom;

    private readonly IOTempGeneratorContext _ctx;

    internal IOTempFileGenerator(IOTempGeneratorContext context) => _ctx = context;

    private static Random GetRandom() => _threadRandom ??= new();

    /// <summary>Runs <paramref name="create" /> inside the duration/success/failure metric triplet for one artifact type, logs, and rethrows on failure.</summary>
    private string TrackCreate(ArtifactMetrics metrics, Func<string> create)
    {
        var sw = Stopwatch.StartNew();
        try {
            var path = create();
            _ctx.Metrics.RecordTiming(metrics.Duration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(metrics.Success);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(metrics.Duration, ex);
            _ctx.Metrics.IncrementCounter(metrics.Failure);
            _ctx.Logger.LogError(ex, "Failed creating {TempArtifact} in session {SessionDirectory}", metrics.Artifact, _ctx.SessionDirectory);
            throw;
        }
    }

    /// <inheritdoc cref="TrackCreate" />
    private async Task<string> TrackCreateAsync(ArtifactMetrics metrics, Func<Task<string>> create)
    {
        var sw = Stopwatch.StartNew();
        try {
            var path = await create().ConfigureAwait(false);
            _ctx.Metrics.RecordTiming(metrics.Duration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(metrics.Success);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(metrics.Duration, ex);
            _ctx.Metrics.IncrementCounter(metrics.Failure);
            _ctx.Logger.LogError(ex, "Failed creating {TempArtifact} in session {SessionDirectory}", metrics.Artifact, _ctx.SessionDirectory);
            throw;
        }
    }

    /// <summary>Checks UTF-8 size of <paramref name="content" />, writes it to a session path, and registers the file for cleanup.</summary>
    private string WriteContent(string content, string? name)
    {
        var sizeBytes = Encoding.UTF8.GetByteCount(content);
        _ctx.ValidateSize(sizeBytes);
        var path = _ctx.ResolvePath(name, false);
        _ctx.Storage.WriteAllText(path, content, Encoding.UTF8);
        _ctx.RegisterFile(path, sizeBytes);
        return path;
    }

    /// <inheritdoc cref="WriteContent" />
    private async Task<string> WriteContentAsync(string content, string? name, CancellationToken ct)
    {
        var sizeBytes = Encoding.UTF8.GetByteCount(content);
        _ctx.ValidateSize(sizeBytes);
        var path = _ctx.ResolvePath(name, false);
        await _ctx.Storage.WriteAllTextAsync(path, content, Encoding.UTF8, ct).ConfigureAwait(false);
        _ctx.RegisterFile(path, sizeBytes);
        return path;
    }

#region Random-bytes files

    /// <inheritdoc />
    public string CreateRandomFile(long sizeBytes, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        return TrackCreate(
            RandomFileMetrics, () => {
                _ctx.ValidateSize(sizeBytes);
                var path = _ctx.ResolvePath(name, false);
                using var stream = _ctx.Storage.OpenCreate(path);
                WriteRandomBytesToStream(stream, sizeBytes, GetRandom());
                _ctx.RegisterFile(path, sizeBytes);
                return path;
            });
    }

    /// <inheritdoc />
    public string CreateRandomFile(FileSizeUnitInfo unit, double amount, string? name = null) => CreateRandomFile(unit.ConvertToBytes(amount), name);

    /// <inheritdoc />
    public IReadOnlyList<string> CreateRandomFiles(int count, long sizeBytes)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(count);
        var paths = new List<string>(count);
        for (var i = 0; i < count; i++)
            paths.Add(CreateRandomFile(sizeBytes));

        return paths;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> CreateRandomFiles(int count, FileSizeUnitInfo unit, double amount) => CreateRandomFiles(count, unit.ConvertToBytes(amount));

    /// <inheritdoc />
    public Task<string> CreateRandomFileAsync(long sizeBytes, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        return TrackCreateAsync(
            RandomFileMetrics, async () => {
                _ctx.ValidateSize(sizeBytes);
                var path = _ctx.ResolvePath(name, false);
                await WriteRandomBytesToStorageAsync(path, sizeBytes, ct).ConfigureAwait(false);
                _ctx.RegisterFile(path, sizeBytes);
                return path;
            });
    }

    /// <inheritdoc />
    public Task<string> CreateRandomFileAsync(FileSizeUnitInfo unit, double amount, string? name = null, CancellationToken ct = default)
        => CreateRandomFileAsync(unit.ConvertToBytes(amount), name, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, long sizeBytes, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(count);
        var paths = new List<string>(count);
        for (var i = 0; i < count; i++) {
            ct.ThrowIfCancellationRequested();
            paths.Add(await CreateRandomFileAsync(sizeBytes, null, ct).ConfigureAwait(false));
        }

        return paths;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, FileSizeUnitInfo unit, double amount, CancellationToken ct = default)
        => CreateRandomFilesAsync(count, unit.ConvertToBytes(amount), ct);

    /// <inheritdoc />
    public IReadOnlyList<string> CreateRandomFiles(int count, long sizeBytes, Func<int, string> nameSelector)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(count);
        ArgumentHelpers.ThrowIfNull(nameSelector);
        var paths = new List<string>(count);
        for (var i = 0; i < count; i++)
            paths.Add(CreateRandomFile(sizeBytes, nameSelector(i)));

        return paths;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, long sizeBytes, Func<int, string> nameSelector, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(count);
        ArgumentHelpers.ThrowIfNull(nameSelector);
        var paths = new List<string>(count);
        for (var i = 0; i < count; i++) {
            ct.ThrowIfCancellationRequested();
            paths.Add(await CreateRandomFileAsync(sizeBytes, nameSelector(i), ct).ConfigureAwait(false));
        }

        return paths;
    }

#endregion

#region Compressed archives

    /// <inheritdoc />
    public string CreateZipFile(TempDirectorySpec spec, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(spec);
        var sw = Stopwatch.StartNew();
        string? path = null;
        try {
            var zipName = name ?? GenerateName(_ctx.Options.FilePrefix, _ctx.Options.FileSuffix, _ctx.Options.FileNamingStrategy) + ".zip";
            path = _ctx.ResolvePath(zipName, false);
            var random = GetRandom();
            using (var zipStream = _ctx.Storage.OpenCreate(path)) {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, false))
                    PopulateZipDirectory(archive, string.Empty, spec, random);
            }

            var sizeBytes = _ctx.Storage.GetFileLength(path);
            _ctx.ValidateSize(sizeBytes);
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateZipFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateZipFileSuccess);
            return path;
        }
        catch (Exception ex) {
            if (path != null && _ctx.Storage.FileExists(path)) {
                try {
                    _ctx.Storage.DeleteFile(path);
                }
                catch { /* best-effort cleanup */
                }
            }

            _ctx.Metrics.RecordError(Constants.Metrics.CreateZipFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateZipFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating zip file in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<string> CreateZipFileAsync(TempDirectorySpec spec, string? name = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => CreateZipFile(spec, name), ct);
    }

    private static void PopulateZipDirectory(ZipArchive archive, string prefix, TempDirectorySpec spec, Random random)
    {
        for (var i = 0; i < spec.FileCount; i++) {
            var sizeBytes = spec.FileSizeSelector != null ? spec.FileSizeSelector(i) : spec.FileSizeBytes;
            var entryName = prefix + Guid.NewGuid().ToString("N") + ".tmp";
            var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
            using var stream = entry.Open();
            WriteRandomBytesToStream(stream, sizeBytes, random);
        }

        if (spec.Subdirectories == null)
            return;

        var dirIdx = 0;
        foreach (var subSpec in spec.Subdirectories)
            PopulateZipDirectory(archive, prefix + "dir_" + dirIdx++ + "/", subSpec, random);
    }

    private static void WriteRandomBytesToStream(Stream stream, long sizeBytes, Random random)
    {
        if (sizeBytes == 0)
            return;

        var bufferSize = (int)Math.Min(RandomChunkSize, sizeBytes);
        var buffer = new byte[bufferSize];
        var remaining = sizeBytes;
        while (remaining > 0) {
            var toWrite = (int)Math.Min(buffer.Length, remaining);
            random.NextBytes(buffer);
            stream.Write(buffer, 0, toWrite);
            remaining -= toWrite;
        }
    }

#endregion

#region Structured content files

    /// <inheritdoc />
    public string CreateTextFile(int lines, int charsPerLine, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(lines);
        ArgumentHelpers.ThrowIfNegativeOrZero(charsPerLine);
        return TrackCreate(TextFileMetrics, () => WriteContent(BuildText(lines, charsPerLine, GetRandom()), name));
    }

    /// <inheritdoc />
    public Task<string> CreateTextFileAsync(int lines, int charsPerLine, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(lines);
        ArgumentHelpers.ThrowIfNegativeOrZero(charsPerLine);
        return TrackCreateAsync(TextFileMetrics, () => WriteContentAsync(BuildText(lines, charsPerLine, GetRandom()), name, ct));
    }

    /// <inheritdoc />
    public string CreateCsvFile(int rows, int columns, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(rows);
        ArgumentHelpers.ThrowIfNegativeOrZero(columns);
        return TrackCreate(CsvFileMetrics, () => WriteContent(BuildCsv(rows, columns, GetRandom()), name));
    }

    /// <inheritdoc />
    public Task<string> CreateCsvFileAsync(int rows, int columns, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(rows);
        ArgumentHelpers.ThrowIfNegativeOrZero(columns);
        return TrackCreateAsync(CsvFileMetrics, () => WriteContentAsync(BuildCsv(rows, columns, GetRandom()), name, ct));
    }

    /// <inheritdoc />
    public string CreateJsonFile(int depth, int keysPerObject, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth);
        ArgumentHelpers.ThrowIfNegativeOrZero(keysPerObject);
        return TrackCreate(JsonFileMetrics, () => WriteContent(BuildJson(depth, keysPerObject, GetRandom()), name));
    }

    /// <inheritdoc />
    public Task<string> CreateJsonFileAsync(int depth, int keysPerObject, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth);
        ArgumentHelpers.ThrowIfNegativeOrZero(keysPerObject);
        return TrackCreateAsync(JsonFileMetrics, () => WriteContentAsync(BuildJson(depth, keysPerObject, GetRandom()), name, ct));
    }

#endregion

#region Directory simulation

    /// <inheritdoc />
    public string SimulateDirectory(TempDirectorySpec spec, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(spec);
        return TrackCreate(
            SimulatedDirectoryMetrics, () => {
                var dirPath = CreateSessionDirectory(name);
                PopulateDirectory(dirPath, spec);
                return dirPath;
            });
    }

    /// <inheritdoc />
    public string SimulateDirectory(int fileCount, long fileSizeBytes, string? name = null) => SimulateDirectory(TempDirectorySpec.Flat(fileCount, fileSizeBytes), name);

    /// <inheritdoc />
    public Task<string> SimulateDirectoryAsync(TempDirectorySpec spec, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(spec);
        return TrackCreateAsync(
            SimulatedDirectoryMetrics, async () => {
                var dirPath = CreateSessionDirectory(name);
                await PopulateDirectoryAsync(dirPath, spec, ct).ConfigureAwait(false);
                return dirPath;
            });
    }

    /// <inheritdoc />
    public Task<string> SimulateDirectoryAsync(int fileCount, long fileSizeBytes, string? name = null, CancellationToken ct = default)
        => SimulateDirectoryAsync(TempDirectorySpec.Flat(fileCount, fileSizeBytes), name, ct);

    /// <inheritdoc />
    public string CreateDirectoryTree(int depth, int filesPerDirectory, long fileSizeBytes, int dirsPerLevel = 2, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth);
        ArgumentHelpers.ThrowIfNegative(filesPerDirectory);
        ArgumentHelpers.ThrowIfNegative(fileSizeBytes);
        ArgumentHelpers.ThrowIfNegative(dirsPerLevel);
        return TrackCreate(DirectoryTreeMetrics, () => SimulateDirectory(BuildTreeSpec(depth, filesPerDirectory, fileSizeBytes, dirsPerLevel), name));
    }

    /// <inheritdoc />
    public Task<string> CreateDirectoryTreeAsync(int depth, int filesPerDirectory, long fileSizeBytes, int dirsPerLevel = 2, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth);
        ArgumentHelpers.ThrowIfNegative(filesPerDirectory);
        ArgumentHelpers.ThrowIfNegative(fileSizeBytes);
        ArgumentHelpers.ThrowIfNegative(dirsPerLevel);
        var spec = BuildTreeSpec(depth, filesPerDirectory, fileSizeBytes, dirsPerLevel);
        return SimulateDirectoryAsync(spec, name, ct);
    }

#endregion

#region XML content files

    /// <inheritdoc />
    public string CreateXmlFile(int depth, int keysPerObject, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth);
        ArgumentHelpers.ThrowIfNegativeOrZero(keysPerObject);
        return TrackCreate(XmlFileMetrics, () => WriteContent(BuildXml(depth, keysPerObject, GetRandom()), name));
    }

    /// <inheritdoc />
    public Task<string> CreateXmlFileAsync(int depth, int keysPerObject, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth);
        ArgumentHelpers.ThrowIfNegativeOrZero(keysPerObject);
        return TrackCreateAsync(XmlFileMetrics, () => WriteContentAsync(BuildXml(depth, keysPerObject, GetRandom()), name, ct));
    }

#endregion

#region Zip extraction

    /// <inheritdoc />
    public string ExtractZipFile(string zipPath, string? targetDirName = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(zipPath);
        if (!_ctx.Storage.FileExists(zipPath))
            throw new FileNotFoundException($"Zip file not found: {zipPath}", zipPath);

        var sw = Stopwatch.StartNew();
        string? destDir = null;
        try {
            // Check each zip entry's size against session caps before writing anything.
            if (_ctx.Options.MaxFileSizeBytes.HasValue) {
                using var preCheck = _ctx.Storage.OpenRead(zipPath);
                using var preArchive = new ZipArchive(preCheck, ZipArchiveMode.Read, false);
                foreach (var entry in preArchive.Entries.Where(e => e.Length > 0)) {
                    if (entry.Length > _ctx.Options.MaxFileSizeBytes.Value) {
                        throw new InvalidOperationException(
                            $"Zip entry '{entry.FullName}' ({entry.Length:N0} bytes) exceeds the per-file size limit of {_ctx.Options.MaxFileSizeBytes.Value:N0} bytes.");
                    }
                }
            }

            var style = _ctx.Storage.PathStyle;
            var dirName = targetDirName ?? PathHelpers.GetFileNameWithoutExtension(style, zipPath) + "_" + Guid.NewGuid().ToString("N")[..8];
            destDir = _ctx.ResolvePath(dirName, true);
            _ctx.Storage.CreateDirectory(destDir);
            using var zipStream = _ctx.Storage.OpenRead(zipPath);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, false);
            var sep = PathHelpers.GetDirectorySeparator(style);
            foreach (var entry in archive.Entries) {
                var relative = entry.FullName.Replace('\\', sep).Replace('/', sep);
                var entryDest = PathHelpers.Combine(style, destDir, relative);
                if (entry.FullName.EndsWith("/", StringComparison.Ordinal) || entry.FullName.EndsWith("\\", StringComparison.Ordinal)) {
                    _ctx.Storage.CreateDirectory(entryDest);
                    _ctx.RegisterDirectory(entryDest);
                }
                else {
                    var parentDir = PathHelpers.GetDirectoryName(style, entryDest);
                    if (!parentDir.IsNullOrEmpty())
                        _ctx.Storage.CreateDirectory(parentDir);

                    using var entryStream = entry.Open();
                    using var destStream = _ctx.Storage.OpenCreate(entryDest);
                    entryStream.CopyTo(destStream);
                    _ctx.RegisterFile(entryDest, entry.Length);
                }
            }

            _ctx.RegisterDirectory(destDir);
            _ctx.Metrics.RecordTiming(Constants.Metrics.ExtractZipFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.ExtractZipFileSuccess);
            return destDir;
        }
        catch (Exception ex) {
            if (destDir != null && _ctx.Storage.DirectoryExists(destDir)) {
                try {
                    _ctx.Storage.DeleteDirectory(destDir);
                }
                catch { /* best effort */
                }
            }

            _ctx.Metrics.RecordError(Constants.Metrics.ExtractZipFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.ExtractZipFileFailure);
            _ctx.Logger.LogError(ex, "Failed extracting zip {ZipPath} in session {SessionDirectory}", zipPath, _ctx.SessionDirectory);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<string> ExtractZipFileAsync(string zipPath, string? targetDirName = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => ExtractZipFile(zipPath, targetDirName), ct);
    }

#endregion

#region Helpers

    private void PopulateDirectory(string dirPath, TempDirectorySpec spec)
    {
        for (var i = 0; i < spec.FileCount; i++) {
            var sizeBytes = spec.FileSizeSelector != null ? spec.FileSizeSelector(i) : spec.FileSizeBytes;
            var fileName = GenerateName(_ctx.Options.FilePrefix, _ctx.Options.FileSuffix, _ctx.Options.FileNamingStrategy) + _ctx.Options.FileExtension;
            var filePath = _ctx.EnsureWithinSession(PathHelpers.Combine(_ctx.Storage.PathStyle, dirPath, fileName));
            _ctx.ValidateSize(sizeBytes);
            using var stream = _ctx.Storage.OpenCreate(filePath);
            WriteRandomBytesToStream(stream, sizeBytes, GetRandom());
            _ctx.RegisterFile(filePath, sizeBytes);
        }

        if (spec.Subdirectories == null)
            return;

        foreach (var subSpec in spec.Subdirectories) {
            var subDirName = GenerateName(_ctx.Options.DirectoryPrefix, _ctx.Options.DirectorySuffix, _ctx.Options.DirectoryNamingStrategy);
            var subDirPath = _ctx.EnsureWithinSession(PathHelpers.Combine(_ctx.Storage.PathStyle, dirPath, subDirName));
            _ctx.Storage.CreateDirectory(subDirPath);
            _ctx.RegisterDirectory(subDirPath);
            PopulateDirectory(subDirPath, subSpec);
        }
    }

    private async Task PopulateDirectoryAsync(string dirPath, TempDirectorySpec spec, CancellationToken ct)
    {
        for (var i = 0; i < spec.FileCount; i++) {
            ct.ThrowIfCancellationRequested();
            var sizeBytes = spec.FileSizeSelector != null ? spec.FileSizeSelector(i) : spec.FileSizeBytes;
            var fileName = GenerateName(_ctx.Options.FilePrefix, _ctx.Options.FileSuffix, _ctx.Options.FileNamingStrategy) + _ctx.Options.FileExtension;
            var filePath = _ctx.EnsureWithinSession(PathHelpers.Combine(_ctx.Storage.PathStyle, dirPath, fileName));
            _ctx.ValidateSize(sizeBytes);
            await WriteRandomBytesToStorageAsync(filePath, sizeBytes, ct).ConfigureAwait(false);
            _ctx.RegisterFile(filePath, sizeBytes);
        }

        if (spec.Subdirectories == null)
            return;

        foreach (var subSpec in spec.Subdirectories) {
            ct.ThrowIfCancellationRequested();
            var subDirName = GenerateName(_ctx.Options.DirectoryPrefix, _ctx.Options.DirectorySuffix, _ctx.Options.DirectoryNamingStrategy);
            var subDirPath = _ctx.EnsureWithinSession(PathHelpers.Combine(_ctx.Storage.PathStyle, dirPath, subDirName));
            _ctx.Storage.CreateDirectory(subDirPath);
            _ctx.RegisterDirectory(subDirPath);
            await PopulateDirectoryAsync(subDirPath, subSpec, ct).ConfigureAwait(false);
        }
    }

    private async Task WriteRandomBytesToStorageAsync(string path, long sizeBytes, CancellationToken ct)
    {
        if (sizeBytes == 0) {
            _ctx.Storage.TouchFile(path);
            return;
        }

        var bufferSize = (int)Math.Min(RandomChunkSize, sizeBytes);
        var buffer = new byte[bufferSize];
        var random = GetRandom();
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        await using var stream = _ctx.Storage.OpenCreate(path);
#else
        using var stream = _ctx.Storage.OpenCreate(path);
#endif
        var remaining = sizeBytes;
        while (remaining > 0) {
            ct.ThrowIfCancellationRequested();
            var toWrite = (int)Math.Min(buffer.Length, remaining);
            random.NextBytes(buffer);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await stream.WriteAsync(buffer.AsMemory(0, toWrite), ct);
#else
            await stream.WriteAsync(buffer, 0, toWrite, ct).ConfigureAwait(false);
#endif
            remaining -= toWrite;
        }
    }

    private static string BuildText(int lines, int charsPerLine, Random random)
    {
        var sb = new StringBuilder(lines * (charsPerLine + 1));
        for (var l = 0; l < lines; l++) {
            var remaining = charsPerLine;
            var first = true;
            while (remaining > 0) {
                var word = WordBank[random.Next(WordBank.Length)];
                if (!first && remaining > 1) {
                    sb.Append(' ');
                    remaining--;
                }

                var toUse = Math.Min(word.Length, remaining);
                sb.Append(word, 0, toUse);
                remaining -= toUse;
                first = false;
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string BuildCsv(int rows, int columns, Random random)
    {
        var sb = new StringBuilder();
        for (var c = 0; c < columns; c++) {
            if (c > 0)
                sb.Append(',');

            sb.Append("col_").Append(c);
        }

        sb.Append('\n');
        for (var r = 0; r < rows; r++) {
            for (var c = 0; c < columns; c++) {
                if (c > 0)
                    sb.Append(',');

                switch (random.Next(3)) {
                    case 0:
                        sb.Append(random.Next(100000));
                        break;
                    case 1:
                        sb.Append((random.NextDouble() * 1000.0).ToString("F2", CultureInfo.InvariantCulture));
                        break;
                    default:
                        sb.Append('"').Append("value_").Append(random.Next(10000)).Append('"');
                        break;
                }
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string BuildJson(int depth, int keysPerObject, Random random)
    {
        var sb = new StringBuilder();
        AppendJsonObject(sb, depth, keysPerObject, random, 0);
        sb.Append('\n');
        return sb.ToString();
    }

    private static void AppendJsonObject(StringBuilder sb, int depth, int keysPerObject, Random random, int indent)
    {
        sb.Append("{\n");
        for (var i = 0; i < keysPerObject; i++) {
            sb.Append(new string(' ', (indent + 1) * 2));
            sb.Append('"').Append("key_").Append(i).Append("\": ");
            if (depth > 0)
                AppendJsonObject(sb, depth - 1, keysPerObject, random, indent + 1);
            else
                AppendJsonLeaf(sb, random);

            if (i < keysPerObject - 1)
                sb.Append(',');

            sb.Append('\n');
        }

        sb.Append(new string(' ', indent * 2)).Append('}');
    }

    private static void AppendJsonLeaf(StringBuilder sb, Random random)
    {
        switch (random.Next(4)) {
            case 0:
                sb.Append(random.Next(10000));
                break;
            case 1:
                sb.Append('"').Append("val_").Append(random.Next(10000)).Append('"');
                break;
            case 2:
                sb.Append(random.Next(2) == 0 ? "true" : "false");
                break;
            default:
                sb.Append((random.NextDouble() * 100.0).ToString("F4", CultureInfo.InvariantCulture));
                break;
        }
    }

    private static string BuildXml(int depth, int keysPerObject, Random random)
    {
        var root = new XElement("root");
        BuildXmlObject(root, depth, keysPerObject, random);
        return root.ToString();
    }

    private static void BuildXmlObject(XElement parent, int depth, int keysPerObject, Random random)
    {
        for (var i = 0; i < keysPerObject; i++) {
            var child = new XElement($"key_{i}");
            if (depth > 0)
                BuildXmlObject(child, depth - 1, keysPerObject, random);
            else
                child.Value = $"val_{random.Next(10000)}";

            parent.Add(child);
        }
    }

    private static TempDirectorySpec BuildTreeSpec(int depth, int filesPerDir, long fileSize, int dirsPerLevel)
    {
        if (depth == 0)
            return TempDirectorySpec.Flat(filesPerDir, fileSize);

        var subdirs = new List<TempDirectorySpec>(dirsPerLevel);
        for (var i = 0; i < dirsPerLevel; i++)
            subdirs.Add(BuildTreeSpec(depth - 1, filesPerDir, fileSize, dirsPerLevel));

        return new() { FileCount = filesPerDir, FileSizeBytes = fileSize, Subdirectories = subdirs };
    }

    /// <summary>Resolves and creates a directory in the session, then registers it for cleanup.</summary>
    private string CreateSessionDirectory(string? name)
    {
        var dirPath = _ctx.ResolvePath(name, true);
        _ctx.Storage.CreateDirectory(dirPath);
        _ctx.RegisterDirectory(dirPath);
        return dirPath;
    }

    private static string GenerateName(string? prefix, string? suffix, TempNamingStrategy strategy)
    {
        var middle = strategy switch {
            TempNamingStrategy.Guid => Guid.NewGuid().ToString("N"),
            TempNamingStrategy.Timestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            TempNamingStrategy.Sequential => Interlocked.Increment(ref _nameSequence).ToString(),
            TempNamingStrategy.RandomChars => Path.GetRandomFileName().Replace(".", string.Empty),
            var _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
        };

        return $"{prefix}{middle}{suffix}";
    }

#endregion

    /// <summary>Metric names and log label for one artifact type, so timing and counters share one record.</summary>
    private sealed record ArtifactMetrics(string Artifact, string Duration, string Success, string Failure);
}