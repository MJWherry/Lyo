using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.IO.Temp.Enums;
using Microsoft.Extensions.Logging;

namespace Lyo.IO.Temp.Models;

// ReSharper disable once InconsistentNaming
public sealed class IOTempFileGenerator : IIOTempFileGenerator
{
    private const int RandomChunkSize = 81920; // 80 KB write buffer

    private static readonly string[] WordBank = [
        "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog",
        "hello", "world", "temp", "file", "data", "test", "sample", "value",
        "alpha", "beta", "gamma", "delta", "lorem", "ipsum", "dolor", "amet",
        "red", "green", "blue", "black", "white", "fast", "slow", "big", "small"
    ];

    private static long _nameSequence;

    private readonly IOTempGeneratorContext _ctx;

    internal IOTempFileGenerator(IOTempGeneratorContext context)
    {
        _ctx = context;
    }

#region Random-bytes files

    public string CreateRandomFile(long sizeBytes, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        var sw = Stopwatch.StartNew();
        try {
            _ctx.ValidateSize(sizeBytes);
            var path = _ctx.ResolvePath(name, false);
            WriteRandomBytes(path, sizeBytes);
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateRandomFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateRandomFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.CreateRandomFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateRandomFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating random temp file in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    public string CreateRandomFile(FileSizeUnitInfo unit, double amount, string? name = null)
        => CreateRandomFile(unit.ConvertToBytes(amount), name);

    public IReadOnlyList<string> CreateRandomFiles(int count, long sizeBytes)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(count, nameof(count));
        var paths = new List<string>(count);
        for (var i = 0; i < count; i++)
            paths.Add(CreateRandomFile(sizeBytes));
        return paths;
    }

    public IReadOnlyList<string> CreateRandomFiles(int count, FileSizeUnitInfo unit, double amount)
        => CreateRandomFiles(count, unit.ConvertToBytes(amount));

    public async Task<string> CreateRandomFileAsync(long sizeBytes, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        var sw = Stopwatch.StartNew();
        try {
            _ctx.ValidateSize(sizeBytes);
            var path = _ctx.ResolvePath(name, false);
            await WriteRandomBytesAsync(path, sizeBytes, ct).ConfigureAwait(false);
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateRandomFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateRandomFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.CreateRandomFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateRandomFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating random temp file async in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    public Task<string> CreateRandomFileAsync(FileSizeUnitInfo unit, double amount, string? name = null, CancellationToken ct = default)
        => CreateRandomFileAsync(unit.ConvertToBytes(amount), name, ct);

    public async Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, long sizeBytes, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(count, nameof(count));
        var paths = new List<string>(count);
        for (var i = 0; i < count; i++) {
            ct.ThrowIfCancellationRequested();
            paths.Add(await CreateRandomFileAsync(sizeBytes, null, ct).ConfigureAwait(false));
        }

        return paths;
    }

    public Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, FileSizeUnitInfo unit, double amount, CancellationToken ct = default)
        => CreateRandomFilesAsync(count, unit.ConvertToBytes(amount), ct);

    public IReadOnlyList<string> CreateRandomFiles(int count, long sizeBytes, Func<int, string> nameSelector)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(count, nameof(count));
        ArgumentHelpers.ThrowIfNull(nameSelector, nameof(nameSelector));
        var paths = new List<string>(count);
        for (var i = 0; i < count; i++)
            paths.Add(CreateRandomFile(sizeBytes, nameSelector(i)));
        return paths;
    }

    public async Task<IReadOnlyList<string>> CreateRandomFilesAsync(int count, long sizeBytes, Func<int, string> nameSelector, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(count, nameof(count));
        ArgumentHelpers.ThrowIfNull(nameSelector, nameof(nameSelector));
        var paths = new List<string>(count);
        for (var i = 0; i < count; i++) {
            ct.ThrowIfCancellationRequested();
            paths.Add(await CreateRandomFileAsync(sizeBytes, nameSelector(i), ct).ConfigureAwait(false));
        }

        return paths;
    }

#endregion

#region Compressed archives

    public string CreateZipFile(TempDirectorySpec spec, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(spec, nameof(spec));
        var sw = Stopwatch.StartNew();
        string? path = null;
        try {
            var zipName = name ?? (GenerateName(_ctx.Options.FilePrefix, _ctx.Options.FileSuffix, _ctx.Options.FileNamingStrategy) + ".zip");
            path = _ctx.ResolvePath(zipName, false);
            var random = GetRandom();
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
                PopulateZipDirectory(archive, string.Empty, spec, random);

            var sizeBytes = new FileInfo(path).Length;
            _ctx.ValidateSize(sizeBytes);
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateZipFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateZipFileSuccess);
            return path;
        }
        catch (Exception ex) {
            if (path != null && File.Exists(path)) {
                try { File.Delete(path); }
                catch { /* best-effort cleanup */ }
            }

            _ctx.Metrics.RecordError(Constants.Metrics.CreateZipFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateZipFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating zip file in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

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

    public string CreateTextFile(int lines, int charsPerLine, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(lines, nameof(lines));
        ArgumentHelpers.ThrowIfNegativeOrZero(charsPerLine, nameof(charsPerLine));
        var sw = Stopwatch.StartNew();
        try {
            var text = BuildText(lines, charsPerLine, GetRandom());
            var sizeBytes = Encoding.UTF8.GetByteCount(text);
            _ctx.ValidateSize(sizeBytes);
            var path = _ctx.ResolvePath(name, false);
            File.WriteAllText(path, text, Encoding.UTF8);
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateTextFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateTextFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.CreateTextFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateTextFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating text temp file in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    public async Task<string> CreateTextFileAsync(int lines, int charsPerLine, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(lines, nameof(lines));
        ArgumentHelpers.ThrowIfNegativeOrZero(charsPerLine, nameof(charsPerLine));
        var sw = Stopwatch.StartNew();
        try {
            var text = BuildText(lines, charsPerLine, GetRandom());
            var sizeBytes = Encoding.UTF8.GetByteCount(text);
            _ctx.ValidateSize(sizeBytes);
            var path = _ctx.ResolvePath(name, false);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await File.WriteAllTextAsync(path, text, Encoding.UTF8, ct);
#else
            await Task.Run(() => { ct.ThrowIfCancellationRequested(); File.WriteAllText(path, text, Encoding.UTF8); }, ct).ConfigureAwait(false);
#endif
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateTextFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateTextFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.CreateTextFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateTextFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating text temp file async in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    public string CreateCsvFile(int rows, int columns, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(rows, nameof(rows));
        ArgumentHelpers.ThrowIfNegativeOrZero(columns, nameof(columns));
        var sw = Stopwatch.StartNew();
        try {
            var csv = BuildCsv(rows, columns, GetRandom());
            var sizeBytes = Encoding.UTF8.GetByteCount(csv);
            _ctx.ValidateSize(sizeBytes);
            var path = _ctx.ResolvePath(name, false);
            File.WriteAllText(path, csv, Encoding.UTF8);
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateCsvFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateCsvFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.CreateCsvFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateCsvFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating CSV temp file in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    public async Task<string> CreateCsvFileAsync(int rows, int columns, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegativeOrZero(rows, nameof(rows));
        ArgumentHelpers.ThrowIfNegativeOrZero(columns, nameof(columns));
        var sw = Stopwatch.StartNew();
        try {
            var csv = BuildCsv(rows, columns, GetRandom());
            var sizeBytes = Encoding.UTF8.GetByteCount(csv);
            _ctx.ValidateSize(sizeBytes);
            var path = _ctx.ResolvePath(name, false);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await File.WriteAllTextAsync(path, csv, Encoding.UTF8, ct);
#else
            await Task.Run(() => { ct.ThrowIfCancellationRequested(); File.WriteAllText(path, csv, Encoding.UTF8); }, ct).ConfigureAwait(false);
#endif
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateCsvFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateCsvFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.CreateCsvFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateCsvFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating CSV temp file async in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    public string CreateJsonFile(int depth, int keysPerObject, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth, nameof(depth));
        ArgumentHelpers.ThrowIfNegativeOrZero(keysPerObject, nameof(keysPerObject));
        var sw = Stopwatch.StartNew();
        try {
            var json = BuildJson(depth, keysPerObject, GetRandom());
            var sizeBytes = Encoding.UTF8.GetByteCount(json);
            _ctx.ValidateSize(sizeBytes);
            var path = _ctx.ResolvePath(name, false);
            File.WriteAllText(path, json, Encoding.UTF8);
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateJsonFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateJsonFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.CreateJsonFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateJsonFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating JSON temp file in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    public async Task<string> CreateJsonFileAsync(int depth, int keysPerObject, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth, nameof(depth));
        ArgumentHelpers.ThrowIfNegativeOrZero(keysPerObject, nameof(keysPerObject));
        var sw = Stopwatch.StartNew();
        try {
            var json = BuildJson(depth, keysPerObject, GetRandom());
            var sizeBytes = Encoding.UTF8.GetByteCount(json);
            _ctx.ValidateSize(sizeBytes);
            var path = _ctx.ResolvePath(name, false);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await File.WriteAllTextAsync(path, json, Encoding.UTF8, ct);
#else
            await Task.Run(() => { ct.ThrowIfCancellationRequested(); File.WriteAllText(path, json, Encoding.UTF8); }, ct).ConfigureAwait(false);
#endif
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateJsonFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateJsonFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.CreateJsonFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateJsonFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating JSON temp file async in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

#endregion

#region Directory simulation

    public string SimulateDirectory(TempDirectorySpec spec, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(spec, nameof(spec));
        var sw = Stopwatch.StartNew();
        try {
            var dirPath = _ctx.ResolvePath(name, true);
            Directory.CreateDirectory(dirPath);
            _ctx.RegisterDirectory(dirPath);
            PopulateDirectory(dirPath, spec);
            _ctx.Metrics.RecordTiming(Constants.Metrics.SimulateDirectoryDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.SimulateDirectorySuccess);
            return dirPath;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.SimulateDirectoryDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.SimulateDirectoryFailure);
            _ctx.Logger.LogError(ex, "Failed simulating directory in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    public string SimulateDirectory(int fileCount, long fileSizeBytes, string? name = null)
        => SimulateDirectory(TempDirectorySpec.Flat(fileCount, fileSizeBytes), name);

    public async Task<string> SimulateDirectoryAsync(TempDirectorySpec spec, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNull(spec, nameof(spec));
        var sw = Stopwatch.StartNew();
        try {
            var dirPath = _ctx.ResolvePath(name, true);
            Directory.CreateDirectory(dirPath);
            _ctx.RegisterDirectory(dirPath);
            await PopulateDirectoryAsync(dirPath, spec, ct).ConfigureAwait(false);
            _ctx.Metrics.RecordTiming(Constants.Metrics.SimulateDirectoryDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.SimulateDirectorySuccess);
            return dirPath;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.SimulateDirectoryDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.SimulateDirectoryFailure);
            _ctx.Logger.LogError(ex, "Failed simulating directory async in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    public Task<string> SimulateDirectoryAsync(int fileCount, long fileSizeBytes, string? name = null, CancellationToken ct = default)
        => SimulateDirectoryAsync(TempDirectorySpec.Flat(fileCount, fileSizeBytes), name, ct);

    public string CreateDirectoryTree(int depth, int filesPerDirectory, long fileSizeBytes, int dirsPerLevel = 2, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth, nameof(depth));
        ArgumentHelpers.ThrowIfNegative(filesPerDirectory, nameof(filesPerDirectory));
        ArgumentHelpers.ThrowIfNegative(fileSizeBytes, nameof(fileSizeBytes));
        ArgumentHelpers.ThrowIfNegative(dirsPerLevel, nameof(dirsPerLevel));
        var sw = Stopwatch.StartNew();
        try {
            var spec = BuildTreeSpec(depth, filesPerDirectory, fileSizeBytes, dirsPerLevel);
            var result = SimulateDirectory(spec, name);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateDirectoryTreeDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateDirectoryTreeSuccess);
            return result;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.CreateDirectoryTreeDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateDirectoryTreeFailure);
            _ctx.Logger.LogError(ex, "Failed creating directory tree in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    public Task<string> CreateDirectoryTreeAsync(int depth, int filesPerDirectory, long fileSizeBytes, int dirsPerLevel = 2, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth, nameof(depth));
        ArgumentHelpers.ThrowIfNegative(filesPerDirectory, nameof(filesPerDirectory));
        ArgumentHelpers.ThrowIfNegative(fileSizeBytes, nameof(fileSizeBytes));
        ArgumentHelpers.ThrowIfNegative(dirsPerLevel, nameof(dirsPerLevel));
        var spec = BuildTreeSpec(depth, filesPerDirectory, fileSizeBytes, dirsPerLevel);
        return SimulateDirectoryAsync(spec, name, ct);
    }

#endregion

#region XML content files

    public string CreateXmlFile(int depth, int keysPerObject, string? name = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth, nameof(depth));
        ArgumentHelpers.ThrowIfNegativeOrZero(keysPerObject, nameof(keysPerObject));
        var sw = Stopwatch.StartNew();
        try {
            var xml = BuildXml(depth, keysPerObject, GetRandom());
            var sizeBytes = Encoding.UTF8.GetByteCount(xml);
            _ctx.ValidateSize(sizeBytes);
            var path = _ctx.ResolvePath(name, false);
            File.WriteAllText(path, xml, Encoding.UTF8);
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateXmlFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateXmlFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.CreateXmlFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateXmlFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating XML temp file in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

    public async Task<string> CreateXmlFileAsync(int depth, int keysPerObject, string? name = null, CancellationToken ct = default)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNegative(depth, nameof(depth));
        ArgumentHelpers.ThrowIfNegativeOrZero(keysPerObject, nameof(keysPerObject));
        var sw = Stopwatch.StartNew();
        try {
            var xml = BuildXml(depth, keysPerObject, GetRandom());
            var sizeBytes = Encoding.UTF8.GetByteCount(xml);
            _ctx.ValidateSize(sizeBytes);
            var path = _ctx.ResolvePath(name, false);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await File.WriteAllTextAsync(path, xml, Encoding.UTF8, ct);
#else
            await Task.Run(() => { ct.ThrowIfCancellationRequested(); File.WriteAllText(path, xml, Encoding.UTF8); }, ct).ConfigureAwait(false);
#endif
            _ctx.RegisterFile(path, sizeBytes);
            _ctx.Metrics.RecordTiming(Constants.Metrics.CreateXmlFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateXmlFileSuccess);
            return path;
        }
        catch (Exception ex) {
            _ctx.Metrics.RecordError(Constants.Metrics.CreateXmlFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.CreateXmlFileFailure);
            _ctx.Logger.LogError(ex, "Failed creating XML temp file async in session {SessionDirectory}", _ctx.SessionDirectory);
            throw;
        }
    }

#endregion

#region Zip extraction

    public string ExtractZipFile(string zipPath, string? targetDirName = null)
    {
        _ctx.ThrowIfDisposed();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(zipPath, nameof(zipPath));
        if (!File.Exists(zipPath))
            throw new FileNotFoundException($"Zip file not found: {zipPath}", zipPath);

        var sw = Stopwatch.StartNew();
        string? destDir = null;
        try {
            // Pre-validate per-file sizes from zip metadata before writing any bytes
            if (_ctx.Options.MaxFileSizeBytes.HasValue) {
                using var preCheck = ZipFile.OpenRead(zipPath);
                foreach (var entry in preCheck.Entries.Where(e => e.Length > 0)) {
                    if (entry.Length > _ctx.Options.MaxFileSizeBytes.Value)
                        throw new InvalidOperationException(
                            $"Zip entry '{entry.FullName}' ({entry.Length:N0} bytes) exceeds the per-file size limit of {_ctx.Options.MaxFileSizeBytes.Value:N0} bytes.");
                }
            }

            var dirName = targetDirName
                          ?? (Path.GetFileNameWithoutExtension(zipPath) + "_" + Guid.NewGuid().ToString("N")[..8]);
            destDir = _ctx.ResolvePath(dirName, true);
            Directory.CreateDirectory(destDir);
            ZipFile.ExtractToDirectory(zipPath, destDir);

            _ctx.RegisterDirectory(destDir);
            foreach (var dir in Directory.EnumerateDirectories(destDir, "*", SearchOption.AllDirectories))
                _ctx.RegisterDirectory(dir);
            foreach (var file in Directory.EnumerateFiles(destDir, "*", SearchOption.AllDirectories))
                _ctx.RegisterFile(file, new FileInfo(file).Length);

            _ctx.Metrics.RecordTiming(Constants.Metrics.ExtractZipFileDuration, sw.Elapsed);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.ExtractZipFileSuccess);
            return destDir;
        }
        catch (Exception ex) {
            if (destDir != null && Directory.Exists(destDir)) {
                try { Directory.Delete(destDir, true); } catch { /* best effort */ }
            }
            _ctx.Metrics.RecordError(Constants.Metrics.ExtractZipFileDuration, ex);
            _ctx.Metrics.IncrementCounter(Constants.Metrics.ExtractZipFileFailure);
            _ctx.Logger.LogError(ex, "Failed extracting zip {ZipPath} in session {SessionDirectory}", zipPath, _ctx.SessionDirectory);
            throw;
        }
    }

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
            var filePath = _ctx.EnsureWithinSession(Path.Combine(dirPath, fileName));
            _ctx.ValidateSize(sizeBytes);
            WriteRandomBytes(filePath, sizeBytes);
            _ctx.RegisterFile(filePath, sizeBytes);
        }

        if (spec.Subdirectories == null)
            return;

        foreach (var subSpec in spec.Subdirectories) {
            var subDirName = GenerateName(_ctx.Options.DirectoryPrefix, _ctx.Options.DirectorySuffix, _ctx.Options.DirectoryNamingStrategy);
            var subDirPath = _ctx.EnsureWithinSession(Path.Combine(dirPath, subDirName));
            Directory.CreateDirectory(subDirPath);
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
            var filePath = _ctx.EnsureWithinSession(Path.Combine(dirPath, fileName));
            _ctx.ValidateSize(sizeBytes);
            await WriteRandomBytesAsync(filePath, sizeBytes, ct).ConfigureAwait(false);
            _ctx.RegisterFile(filePath, sizeBytes);
        }

        if (spec.Subdirectories == null)
            return;

        foreach (var subSpec in spec.Subdirectories) {
            ct.ThrowIfCancellationRequested();
            var subDirName = GenerateName(_ctx.Options.DirectoryPrefix, _ctx.Options.DirectorySuffix, _ctx.Options.DirectoryNamingStrategy);
            var subDirPath = _ctx.EnsureWithinSession(Path.Combine(dirPath, subDirName));
            Directory.CreateDirectory(subDirPath);
            _ctx.RegisterDirectory(subDirPath);
            await PopulateDirectoryAsync(subDirPath, subSpec, ct).ConfigureAwait(false);
        }
    }

    private static void WriteRandomBytes(string path, long sizeBytes)
    {
        if (sizeBytes == 0) {
            File.Create(path).Dispose();
            return;
        }

        var bufferSize = (int)Math.Min(RandomChunkSize, sizeBytes);
        var buffer = new byte[bufferSize];
        var random = GetRandom();

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize);
        var remaining = sizeBytes;
        while (remaining > 0) {
            var toWrite = (int)Math.Min(buffer.Length, remaining);
            random.NextBytes(buffer);
            fs.Write(buffer, 0, toWrite);
            remaining -= toWrite;
        }
    }

    private static async Task WriteRandomBytesAsync(string path, long sizeBytes, CancellationToken ct)
    {
        if (sizeBytes == 0) {
            File.Create(path).Dispose();
            return;
        }

        var bufferSize = (int)Math.Min(RandomChunkSize, sizeBytes);
        var buffer = new byte[bufferSize];
        var random = GetRandom();

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize);
#else
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize);
#endif
        var remaining = sizeBytes;
        while (remaining > 0) {
            ct.ThrowIfCancellationRequested();
            var toWrite = (int)Math.Min(buffer.Length, remaining);
            random.NextBytes(buffer);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await fs.WriteAsync(buffer.AsMemory(0, toWrite), ct);
#else
            await fs.WriteAsync(buffer, 0, toWrite, ct).ConfigureAwait(false);
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
            if (c > 0) sb.Append(',');
            sb.Append("col_").Append(c);
        }

        sb.Append('\n');

        for (var r = 0; r < rows; r++) {
            for (var c = 0; c < columns; c++) {
                if (c > 0) sb.Append(',');
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

        return new TempDirectorySpec {
            FileCount = filesPerDir,
            FileSizeBytes = fileSize,
            Subdirectories = subdirs
        };
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

#if NET6_0_OR_GREATER
    private static Random GetRandom() => Random.Shared;
#else
    [ThreadStatic]
    private static Random? _threadRandom;

    private static Random GetRandom() => _threadRandom ??= new Random();
#endif

#endregion
}
