using System.Text;
using System.Text.RegularExpressions;
using Lyo.Common;
using Lyo.Common.Records;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Pdf.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf.IO;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Tokens;

namespace Lyo.Pdf;

public class PdfService : IPdfService, IDisposable
{
    private readonly ReaderWriterLockSlim _documentsLock = new(LockRecursionPolicy.SupportsRecursion);
    private readonly HttpClient? _httpClient;
    private readonly Dictionary<Guid, LoadedPdf> _loadedDocuments = new();
    private readonly ILogger<PdfService> _logger;
    private readonly long _maxPdfSizeBytes;
    private readonly long _maxTotalLoadedBytes;
    private readonly IMetrics _metrics;
    private readonly PdfServiceOptions _options;
    private bool _disposed;
    private long _totalLoadedBytes;

    public PdfService(ILogger<PdfService>? logger = null, IMetrics? metrics = null, HttpClient? httpClient = null, PdfServiceOptions? options = null)
    {
        _logger = logger ?? NullLogger<PdfService>.Instance;
        _options = options ?? new PdfServiceOptions();
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _httpClient = httpClient;
        _maxPdfSizeBytes = _options.MaxPdfSizeBytes.GetValueOrDefault() > 0 ? _options.MaxPdfSizeBytes!.Value : PdfServiceOptions.SuggestedMaxPdfSizeBytes;
        _maxTotalLoadedBytes = _options.MaxTotalLoadedBytes.GetValueOrDefault() > 0 ? _options.MaxTotalLoadedBytes!.Value : PdfServiceOptions.SuggestedMaxTotalLoadedBytes;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _documentsLock.EnterWriteLock();
        try {
            foreach (var loadedPdf in _loadedDocuments.Values) {
                try {
                    loadedPdf.Document.Dispose();
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Error disposing PDF document {PdfId}", loadedPdf.Id);
                }
            }

            _loadedDocuments.Clear();
            _totalLoadedBytes = 0;
        }
        finally {
            _documentsLock.ExitWriteLock();
            _documentsLock.Dispose();
        }

        _disposed = true;
    }

    /// <inheritdoc />
    public async Task<LoadedPdfLease> LoadPdfFromFileAsync(string filePath, CancellationToken ct = default)
        => CreateLease(await LoadPdfFromFileIdAsync(filePath, ct).ConfigureAwait(false));

    /// <inheritdoc />
    public LoadedPdfLease LoadPdfFromFile(string filePath) => CreateLease(LoadPdfFromFileId(filePath));

    /// <inheritdoc />
    public async Task<IReadOnlyList<LoadedPdfLease>> LoadPdfsFromFilesAsync(IEnumerable<string> filePaths, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(filePaths, nameof(filePaths));
        var paths = filePaths.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(paths, nameof(filePaths));
        foreach (var path in paths)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(filePaths));

        return await LoadBatchLeasesAsync(paths, LoadPdfFromFileIdAsync, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IReadOnlyList<LoadedPdfLease> LoadPdfsFromFiles(params string[] filePaths)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(filePaths, nameof(filePaths));
        foreach (var path in filePaths)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(filePaths));

        return LoadBatchLeases(filePaths, LoadPdfFromFileId);
    }

    public async Task<LoadedPdfLease> LoadPdfFromUrlAsync(string url, CancellationToken ct = default) => CreateLease(await LoadPdfFromUrlIdAsync(url, ct).ConfigureAwait(false));

    public LoadedPdfLease LoadPdfFromUrl(string url) => CreateLease(LoadPdfFromUrlId(url));

    public async Task<IReadOnlyList<LoadedPdfLease>> LoadPdfsFromUrlsAsync(IEnumerable<string> urls, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(urls, nameof(urls));
        var urlList = urls.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(urlList, nameof(urls));
        foreach (var url in urlList)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(url, nameof(urls));

        return await LoadBatchLeasesAsync(urlList, LoadPdfFromUrlIdAsync, ct).ConfigureAwait(false);
    }

    public IReadOnlyList<LoadedPdfLease> LoadPdfsFromUrls(params string[] urls)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(urls, nameof(urls));
        foreach (var url in urls)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(url, nameof(urls));

        return LoadBatchLeases(urls, LoadPdfFromUrlId);
    }

    public async Task<LoadedPdfLease> LoadPdfFromBytesAsync(byte[] pdfBytes, CancellationToken ct = default)
        => CreateLease(await LoadPdfFromBytesIdAsync(pdfBytes, ct).ConfigureAwait(false));

    public LoadedPdfLease LoadPdfFromBytes(byte[] pdfBytes) => CreateLease(LoadPdfFromBytesId(pdfBytes));

    public async Task<IReadOnlyList<LoadedPdfLease>> LoadPdfsFromBytesAsync(IEnumerable<byte[]> pdfs, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(pdfs, nameof(pdfs));
        var pdfList = pdfs.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(pdfList, nameof(pdfs));
        foreach (var pdfBytes in pdfList)
            ArgumentHelpers.ThrowIfNull(pdfBytes, nameof(pdfs));

        return await LoadBatchLeasesAsync(pdfList, LoadPdfFromBytesIdAsync, ct).ConfigureAwait(false);
    }

    public IReadOnlyList<LoadedPdfLease> LoadPdfsFromBytes(params byte[][] pdfs)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(pdfs, nameof(pdfs));
        foreach (var pdfBytes in pdfs)
            ArgumentHelpers.ThrowIfNullOrEmpty(pdfBytes, nameof(pdfs));

        return LoadBatchLeases(pdfs, LoadPdfFromBytesId);
    }

    public async Task<LoadedPdfLease> LoadPdfFromStreamAsync(Stream stream, CancellationToken ct = default)
        => CreateLease(await LoadPdfFromStreamIdAsync(stream, ct).ConfigureAwait(false));

    public LoadedPdfLease LoadPdfFromStream(Stream stream) => CreateLease(LoadPdfFromStreamId(stream));

    public async Task<IReadOnlyList<LoadedPdfLease>> LoadPdfsFromStreamsAsync(IEnumerable<Stream> streams, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(streams, nameof(streams));
        var streamList = streams.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(streamList, nameof(streams));
        foreach (var stream in streamList)
            ArgumentHelpers.ThrowIfNull(stream, nameof(streams));

        return await LoadBatchLeasesAsync(streamList, LoadPdfFromStreamIdAsync, ct).ConfigureAwait(false);
    }

    public IReadOnlyList<LoadedPdfLease> LoadPdfsFromStreams(params Stream[] streams)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(streams, nameof(streams));
        foreach (var stream in streams)
            ArgumentHelpers.ThrowIfNull(stream, nameof(streams));

        return LoadBatchLeases(streams, LoadPdfFromStreamId);
    }

    public async Task UnloadPdfAsync(Guid pdfId, CancellationToken ct = default) => await Task.Run(() => UnloadPdfInternal(pdfId), ct).ConfigureAwait(false);

    public void UnloadPdf(Guid pdfId) => UnloadPdfInternal(pdfId);

    public async Task<PdfInfo> GetPdfInfoAsync(Guid pdfId, CancellationToken ct = default) => await Task.Run(() => GetPdfInfoInternal(pdfId), ct).ConfigureAwait(false);

    public PdfInfo GetPdfInfo(Guid pdfId) => GetPdfInfoInternal(pdfId);

    // ── PDF Content Extraction ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<PdfWord>> GetWordsAsync(Guid pdfId, int? page = null, CancellationToken ct = default)
        => await Task.Run(() => GetWordsInternal(pdfId, page), ct).ConfigureAwait(false);

    public IReadOnlyList<PdfWord> GetWords(Guid pdfId, int? page = null) => GetWordsInternal(pdfId, page);

    public async Task<IReadOnlyList<PdfTextLine>> GetLinesAsync(Guid pdfId, int? page = null, double? yTolerance = null, CancellationToken ct = default)
        => await Task.Run(() => GetLinesInternal(pdfId, page, yTolerance), ct).ConfigureAwait(false);

    public IReadOnlyList<PdfTextLine> GetLines(Guid pdfId, int? page = null, double? yTolerance = null) => GetLinesInternal(pdfId, page, yTolerance);

    public async Task<IReadOnlyList<PdfWord>> GetWordsBetweenAsync(Guid pdfId, string? startText = null, string? endText = null, int? page = null, CancellationToken ct = default)
        => await Task.Run(() => GetWordsBetweenInternal(pdfId, startText, endText, page), ct).ConfigureAwait(false);

    public IReadOnlyList<PdfWord> GetWordsBetween(Guid pdfId, string? startText = null, string? endText = null, int? page = null)
        => GetWordsBetweenInternal(pdfId, startText, endText, page);

    public async Task<IReadOnlyList<PdfTextLine>> GetLinesBetweenAsync(
        Guid pdfId,
        string? startText = null,
        string? endText = null,
        int? page = null,
        double? yTolerance = null,
        CancellationToken ct = default)
        => await Task.Run(() => GetLinesBetweenInternal(pdfId, startText, endText, page, yTolerance), ct).ConfigureAwait(false);

    public IReadOnlyList<PdfTextLine> GetLinesBetween(Guid pdfId, string? startText = null, string? endText = null, int? page = null, double? yTolerance = null)
        => GetLinesBetweenInternal(pdfId, startText, endText, page, yTolerance);

    public async Task<IReadOnlyList<PdfTextLine>> GetLinesInBoundingBoxAsync(Guid pdfId, PdfBoundingBox region, double? yTolerance = null, CancellationToken ct = default)
        => await Task.Run(() => GetLinesInBoundingBoxInternal(pdfId, region, yTolerance), ct).ConfigureAwait(false);

    public IReadOnlyList<PdfTextLine> GetLinesInBoundingBox(Guid pdfId, PdfBoundingBox region, double? yTolerance = null)
        => GetLinesInBoundingBoxInternal(pdfId, region, yTolerance);

    public async Task<PdfColumnarText> GetColumnarTextInBoundingBoxAsync(
        Guid pdfId,
        PdfBoundingBox region,
        int columnCount,
        double? yTolerance = null,
        CancellationToken ct = default)
        => await Task.Run(() => GetColumnarTextInBoundingBox(pdfId, region, columnCount, yTolerance), ct).ConfigureAwait(false);

    public PdfColumnarText GetColumnarTextInBoundingBox(Guid pdfId, PdfBoundingBox region, int columnCount, double? yTolerance = null)
    {
        var lines = GetLinesInBoundingBoxInternal(pdfId, region, yTolerance);
        var words = lines.SelectMany(l => l.Words).ToList();
        return BuildColumnarText(words, columnCount, yTolerance);
    }

    public async Task<PdfColumnarText> GetColumnarTextAsync(IReadOnlyList<PdfWord> words, int columnCount, double? yTolerance = null, CancellationToken ct = default)
        => await Task.Run(() => GetColumnarText(words, columnCount, yTolerance), ct).ConfigureAwait(false);

    public PdfColumnarText GetColumnarText(IReadOnlyList<PdfWord> words, int columnCount, double? yTolerance = null)
    {
        ArgumentHelpers.ThrowIfNull(words, nameof(words));
        return BuildColumnarText(words.ToList(), columnCount, yTolerance);
    }

    public async Task<IReadOnlyList<KvColumnResult>> ExtractKeyValuePairsAsync(
        Guid pdfId,
        IEnumerable<string> knownKeys,
        int? page = null,
        double yTolerance = 5.0,
        PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
        int keyValueColumnCount = 1,
        CancellationToken ct = default)
        => await Task.Run(() => ExtractKeyValuePairsInternal(pdfId, knownKeys, page, yTolerance, keyValueLayout, keyValueColumnCount), ct).ConfigureAwait(false);

    public IReadOnlyList<KvColumnResult> ExtractKeyValuePairs(
        Guid pdfId,
        IEnumerable<string> knownKeys,
        int? page = null,
        double yTolerance = 5.0,
        PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
        int keyValueColumnCount = 1)
        => ExtractKeyValuePairsInternal(pdfId, knownKeys, page, yTolerance, keyValueLayout, keyValueColumnCount);

    public async Task<IReadOnlyList<KvColumnResult>> ExtractKeyValuePairsAsync(
        IReadOnlyList<PdfWord> words,
        IEnumerable<string> knownKeys,
        double yTolerance = 5.0,
        PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
        int keyValueColumnCount = 1,
        CancellationToken ct = default)
        => await Task.Run(() => ExtractKeyValuePairs(words, knownKeys, yTolerance, keyValueLayout, keyValueColumnCount), ct).ConfigureAwait(false);

    public IReadOnlyList<KvColumnResult> ExtractKeyValuePairs(
        IReadOnlyList<PdfWord> words,
        IEnumerable<string> knownKeys,
        double yTolerance = 5.0,
        PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
        int keyValueColumnCount = 1)
        => ExecuteWithMetrics(
            Constants.Metrics.ExtractKeyValueDuration, Constants.Metrics.ExtractKeyValueSuccess, Constants.Metrics.ExtractKeyValueFailure, () => {
                ArgumentHelpers.ThrowIfNull(words, nameof(words));
                ArgumentHelpers.ThrowIfNull(knownKeys, nameof(knownKeys));
                return ExtractKeyValueColumns(words.ToList(), knownKeys, yTolerance, keyValueLayout, keyValueColumnCount);
            });

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ExtractTableAsync(
        Guid pdfId,
        ColumnHeader[] headers,
        int? page = null,
        double yTolerance = 5.0,
        CancellationToken ct = default)
        => await Task.Run(() => ExtractTableInternal(pdfId, headers, page, yTolerance), ct).ConfigureAwait(false);

    public IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTable(Guid pdfId, ColumnHeader[] headers, int? page = null, double yTolerance = 5.0)
        => ExtractTableInternal(pdfId, headers, page, yTolerance);

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ExtractTableAsync(
        IReadOnlyList<PdfWord> words,
        ColumnHeader[] headers,
        double yTolerance = 5.0,
        PdfInferFormattingFlags? inferFormattingForHeaderRows = null,
        CancellationToken ct = default)
        => await Task.Run(() => ExtractTable(words, headers, yTolerance, inferFormattingForHeaderRows), ct).ConfigureAwait(false);

    public IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTable(
        IReadOnlyList<PdfWord> words,
        ColumnHeader[] headers,
        double yTolerance = 5.0,
        PdfInferFormattingFlags? inferFormattingForHeaderRows = null)
        => ExecuteWithMetrics(
            Constants.Metrics.ExtractTableDuration, Constants.Metrics.ExtractTableSuccess, Constants.Metrics.ExtractTableFailure, () => {
                ArgumentHelpers.ThrowIfNull(words, nameof(words));
                ArgumentHelpers.ThrowIfNull(headers, nameof(headers));
                return ExtractTableFromWords(words.ToList(), headers, yTolerance, inferFormattingForHeaderRows);
            });

    public IReadOnlyDictionary<string, string?> InferKeyValuePairsFromFormatting(
        IReadOnlyList<PdfWord> words,
        double yTolerance = 5.0,
        int columnCount = 1,
        PdfInferFormattingFlags inferFlags = PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Semicolon | PdfInferFormattingFlags.Underline)
    {
        ArgumentHelpers.ThrowIfNull(words, nameof(words));
        if (words.Count == 0 || inferFlags == PdfInferFormattingFlags.None)
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var list = words.ToList();
        var bands = ClampKvColumnBandCount(columnCount);
        if (bands <= 1)
            return InferKeyValuePairsFromFormattingInternal(list, yTolerance, inferFlags);

        var columns = BandWordsIntoVerticalColumns(list, bands);
        var merged = new List<KvColumnResult>(columns.Count);
        for (var i = 0; i < columns.Count; i++) {
            var dict = InferKeyValuePairsFromFormattingInternal(columns[i], yTolerance, inferFlags);
            merged.Add(new(i, dict));
        }

        return KvColumnResult.Merge(merged);

    }

    public ColumnHeader[] InferTableHeadersFromFormatting(
        IReadOnlyList<PdfWord> words,
        double? yTolerance = null,
        PdfInferFormattingFlags inferFlags = PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Semicolon | PdfInferFormattingFlags.Underline)
    {
        ArgumentHelpers.ThrowIfNull(words, nameof(words));
        return InferTableHeadersFromFormattingInternal(words.ToList(), yTolerance, inferFlags);
    }

    public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] pdfBytes, ColumnHeader[] headers, int? page = null, double yTolerance = 5.0)
    {
        ArgumentHelpers.ThrowIfNull(pdfBytes, nameof(pdfBytes));
        ArgumentHelpers.ThrowIfNull(headers, nameof(headers));
        if (headers.Length == 0)
            return Result<DataTable.Models.DataTable>.Failure("At least one column header is required.", "PdfTabular.NoHeaders");

        try {
            using var lease = LoadPdfFromBytes(pdfBytes);
            var (headerCells, formattedRows) = ExtractTableFormattedInternal(lease.Id, headers, page, yTolerance);
            return Result<DataTable.Models.DataTable>.Success(RowsToDataTable(headers, formattedRows, headerCells));
        }
        catch (Exception ex) {
            return Result<DataTable.Models.DataTable>.Failure(ex);
        }
    }

    public async Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(
        byte[] pdfBytes,
        ColumnHeader[] headers,
        int? page = null,
        double yTolerance = 5.0,
        CancellationToken ct = default)
        => await Task.Run(() => ParseBytesAsDataTable(pdfBytes, headers, page, yTolerance), ct).ConfigureAwait(false);

    public async Task<byte[]> MergePdfsToFileAsync(IEnumerable<Guid> pdfIds, string filePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        ct.ThrowIfCancellationRequested();
        var bytes = await MergePdfsAsync(pdfIds, ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await fileStream.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
        await fileStream.FlushAsync(ct).ConfigureAwait(false);
        return bytes;
    }

    public byte[] MergePdfsToFile(IEnumerable<Guid> pdfIds, string filePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        var bytes = MergePdfs(pdfIds);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllBytes(filePath, bytes);
        return bytes;
    }

    public async Task<byte[]> MergePdfsToStreamAsync(IEnumerable<Guid> pdfIds, Stream stream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream, nameof(stream));
        OperationHelpers.ThrowIfNotWritable(stream, $"Stream '{nameof(stream)}' must be writable.");
        var bytes = await MergePdfsAsync(pdfIds, ct).ConfigureAwait(false);
        if (stream.CanSeek)
            stream.Position = 0;

        await stream.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
        return bytes;
    }

    public byte[] MergePdfsToStream(IEnumerable<Guid> pdfIds, Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream, nameof(stream));
        OperationHelpers.ThrowIfNotWritable(stream, $"Stream '{nameof(stream)}' must be writable.");
        var bytes = MergePdfs(pdfIds);
        if (stream.CanSeek)
            stream.Position = 0;

        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
        return bytes;
    }

    public async Task<byte[]> MergePdfsAsync(IEnumerable<Guid> pdfIds, CancellationToken ct = default) => await Task.Run(() => MergePdfsInternal(pdfIds), ct).ConfigureAwait(false);

    public byte[] MergePdfs(IEnumerable<Guid> pdfIds) => MergePdfsInternal(pdfIds);

    public async Task<byte[]> MergePdfFilesAsync(string outputFilePath, string initialPdf, string[] paths, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath, nameof(outputFilePath));
        var allPaths = BuildMergePathInputs(initialPdf, paths);
        var ids = new List<Guid>(allPaths.Count);
        try {
            foreach (var path in allPaths)
                ids.Add(await LoadPdfFromFileIdAsync(path, ct).ConfigureAwait(false));

            return await MergePdfsToFileAsync(ids, outputFilePath, ct).ConfigureAwait(false);
        }
        finally {
            UnloadLoadedPdfs(ids);
        }
    }

    public byte[] MergePdfFiles(string outputFilePath, string initialPdf, params string[] paths)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath, nameof(outputFilePath));
        var allPaths = BuildMergePathInputs(initialPdf, paths);
        var ids = new List<Guid>(allPaths.Count);
        try {
            foreach (var path in allPaths)
                ids.Add(LoadPdfFromFileId(path));

            return MergePdfsToFile(ids, outputFilePath);
        }
        finally {
            UnloadLoadedPdfs(ids);
        }
    }

    public Task<byte[]> MergePdfFilesAsync(string outputFilePath, FileInfo initialPdf, FileInfo[] paths, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(initialPdf, nameof(initialPdf));
        ArgumentHelpers.ThrowIfNull(paths, nameof(paths));
        var fullPaths = paths.Select(p => p.FullName).ToArray();
        return MergePdfFilesAsync(outputFilePath, initialPdf.FullName, fullPaths, ct);
    }

    public byte[] MergePdfFiles(string outputFilePath, FileInfo initialPdf, params FileInfo[] paths)
    {
        ArgumentHelpers.ThrowIfNull(initialPdf, nameof(initialPdf));
        ArgumentHelpers.ThrowIfNull(paths, nameof(paths));
        var fullPaths = paths.Select(p => p.FullName).ToArray();
        return MergePdfFiles(outputFilePath, initialPdf.FullName, fullPaths);
    }

    public async Task<byte[]> MergePdfBytesAsync(string outputFilePath, byte[] initialPdf, byte[][] pdfs, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath, nameof(outputFilePath));
        var allPdfs = BuildMergeByteInputs(initialPdf, pdfs);
        var ids = new List<Guid>(allPdfs.Count);
        try {
            foreach (var pdfBytes in allPdfs)
                ids.Add(await LoadPdfFromBytesIdAsync(pdfBytes, ct).ConfigureAwait(false));

            return await MergePdfsToFileAsync(ids, outputFilePath, ct).ConfigureAwait(false);
        }
        finally {
            UnloadLoadedPdfs(ids);
        }
    }

    public byte[] MergePdfBytes(string outputFilePath, byte[] initialPdf, params byte[][] pdfs)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath, nameof(outputFilePath));
        var allPdfs = BuildMergeByteInputs(initialPdf, pdfs);
        var ids = new List<Guid>(allPdfs.Count);
        try {
            foreach (var pdfBytes in allPdfs)
                ids.Add(LoadPdfFromBytesId(pdfBytes));

            return MergePdfsToFile(ids, outputFilePath);
        }
        finally {
            UnloadLoadedPdfs(ids);
        }
    }

    public async Task SavePdfAsync(Guid pdfId, string filePath, CancellationToken ct = default)
        => await Task.Run(
                async () => {
                    using var timer = _metrics.StartTimer(Constants.Metrics.SaveDuration);
                    try {
                        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
                        var bytes = GetPdfBytesInternal(pdfId);
                        var directory = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

#if NETSTANDARD2_0
                        File.WriteAllBytes(filePath, bytes);
#else
                        await File.WriteAllBytesAsync(filePath, bytes, ct).ConfigureAwait(false);
#endif
                        _logger.LogDebug("Saved PDF with ID {PdfId} to {FilePath}", pdfId, filePath);
                        _metrics.IncrementCounter(Constants.Metrics.SaveSuccess);
                    }
                    catch (Exception ex) {
                        _metrics.IncrementCounter(Constants.Metrics.SaveFailure);
                        _metrics.RecordError(Constants.Metrics.SaveDuration, ex);
                        throw;
                    }
                }, ct)
            .ConfigureAwait(false);

    public void SavePdf(Guid pdfId, string filePath) => SavePdfInternal(pdfId, filePath);

    public async Task SavePdfToStreamAsync(Guid pdfId, Stream stream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream, nameof(stream));
        OperationHelpers.ThrowIfNotWritable(stream, $"Stream '{nameof(stream)}' must be writable.");
        var bytes = await GetPdfBytesAsync(pdfId, ct).ConfigureAwait(false);
        if (stream.CanSeek)
            stream.Position = 0;

        await stream.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("Saved PDF with ID {PdfId} to stream", pdfId);
    }

    public void SavePdfToStream(Guid pdfId, Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream, nameof(stream));
        OperationHelpers.ThrowIfNotWritable(stream, $"Stream '{nameof(stream)}' must be writable.");
        var bytes = GetPdfBytes(pdfId);
        if (stream.CanSeek)
            stream.Position = 0;

        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
        _logger.LogDebug("Saved PDF with ID {PdfId} to stream", pdfId);
    }

    public async Task<byte[]> GetPdfBytesAsync(Guid pdfId, CancellationToken ct = default) => await Task.Run(() => GetPdfBytesInternal(pdfId), ct).ConfigureAwait(false);

    public byte[] GetPdfBytes(Guid pdfId) => GetPdfBytesInternal(pdfId);

    /// <summary>
    /// Gets all words between the start section and the next known section, spanning multiple pages. For each page, the end boundary is determined by scanning forward for the
    /// first line that matches any section that comes after the start section in the document order.
    /// </summary>
    /// <param name="pdfService">The PDF service instance.</param>
    /// <param name="pdfId">The loaded PDF identifier.</param>
    /// <param name="startSection">The section header to start from (e.g. "CHARGES").</param>
    /// <param name="sectionsInOrder">All section headers in document order (top to bottom). The end boundary is the first section after startSection that appears on each page.</param>
    /// <param name="defaultEndSection">When no end section is found on a page, use this as the end marker. If null, takes content to end of page.</param>
    /// <param name="startPage">Optional first page to process (1-based). Default 1.</param>
    /// <param name="endPage">Optional last page to process (1-based). Default last page.</param>
    /// <returns>All words from the section across the specified pages.</returns>
    public IReadOnlyList<PdfWord> GetWordsBetweenSections(
        Guid pdfId,
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null)
    {
        ArgumentHelpers.ThrowIfNull(startSection, nameof(startSection));
        ArgumentHelpers.ThrowIfNull(sectionsInOrder, nameof(sectionsInOrder));
        var sections = sectionsInOrder.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        var startIdx = sections.FindIndex(s => string.Equals(s, startSection.Trim(), StringComparison.OrdinalIgnoreCase));
        var endCandidates = startIdx >= 0 && startIdx < sections.Count - 1 ? sections.Skip(startIdx + 1).ToArray() : [];
        var pageCount = GetPdfInfo(pdfId).PageCount;
        var firstPage = startPage ?? 1;
        var lastPage = endPage ?? pageCount;
        firstPage = Math.Max(1, firstPage);
        lastPage = Math.Min(pageCount, lastPage);
        var result = new List<PdfWord>();
        for (var pageNum = firstPage; pageNum <= lastPage; pageNum++) {
            var pageLines = GetLines(pdfId, pageNum);
            var sectionStartIdx = -1;
            for (var i = 0; i < pageLines.Count; i++) {
                if (!pageLines[i].Text.Trim().StartsWith(startSection.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                sectionStartIdx = i;
                break;
            }

            if (sectionStartIdx < 0)
                continue;

            string? endSection = null;
            for (var i = sectionStartIdx + 1; i < pageLines.Count; i++) {
                var lineText = pageLines[i].Text.Trim();
                var found = endCandidates.FirstOrDefault(s => lineText.StartsWith(s, StringComparison.OrdinalIgnoreCase));
                if (found == null)
                    continue;

                endSection = found;
                break;
            }

            endSection ??= defaultEndSection;
            var pageWords = GetWordsBetween(pdfId, startSection, endSection, pageNum);
            result.AddRange(pageWords);
        }

        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<PdfTextLine> GetLinesBetweenSections(
        Guid pdfId,
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null)
    {
        ArgumentHelpers.ThrowIfNull(startSection, nameof(startSection));
        ArgumentHelpers.ThrowIfNull(sectionsInOrder, nameof(sectionsInOrder));
        var sections = sectionsInOrder.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        var startIdx = sections.FindIndex(s => string.Equals(s, startSection.Trim(), StringComparison.OrdinalIgnoreCase));
        var endCandidates = startIdx >= 0 && startIdx < sections.Count - 1 ? sections.Skip(startIdx + 1).ToArray() : [];
        var pageCount = GetPdfInfo(pdfId).PageCount;
        var firstPage = startPage ?? 1;
        var lastPage = endPage ?? pageCount;
        firstPage = Math.Max(1, firstPage);
        lastPage = Math.Min(pageCount, lastPage);
        var result = new List<PdfTextLine>();
        for (var pageNum = firstPage; pageNum <= lastPage; pageNum++) {
            var pageLines = GetLines(pdfId, pageNum, yTolerance);
            var sectionStartIdx = -1;
            for (var i = 0; i < pageLines.Count; i++) {
                if (!pageLines[i].Text.Trim().StartsWith(startSection.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                sectionStartIdx = i;
                break;
            }

            if (sectionStartIdx < 0)
                continue;

            string? endSection = null;
            for (var i = sectionStartIdx + 1; i < pageLines.Count; i++) {
                var lineText = pageLines[i].Text.Trim();
                var found = endCandidates.FirstOrDefault(s => lineText.StartsWith(s, StringComparison.OrdinalIgnoreCase));
                if (found == null)
                    continue;

                endSection = found;
                break;
            }

            endSection ??= defaultEndSection;
            var sectionLines = GetLinesBetween(pdfId, startSection, endSection, pageNum, yTolerance);
            result.AddRange(sectionLines);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PdfTextLine>> GetLinesBetweenSectionsAsync(
        Guid pdfId,
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null,
        CancellationToken ct = default)
        => await Task.Run(() => GetLinesBetweenSections(pdfId, startSection, sectionsInOrder, defaultEndSection, startPage, endPage, yTolerance), ct).ConfigureAwait(false);

    /// <inheritdoc />
    public PdfSection? GetSection(
        Guid pdfId,
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null)
    {
        ArgumentHelpers.ThrowIfNull(startSection, nameof(startSection));
        ArgumentHelpers.ThrowIfNull(sectionsInOrder, nameof(sectionsInOrder));
        var sections = sectionsInOrder.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        var startIdx = sections.FindIndex(s => string.Equals(s, startSection.Trim(), StringComparison.OrdinalIgnoreCase));
        var endCandidates = startIdx >= 0 && startIdx < sections.Count - 1 ? sections.Skip(startIdx + 1).ToArray() : [];
        var pageCount = GetPdfInfo(pdfId).PageCount;
        var firstPage = startPage ?? 1;
        var lastPage = endPage ?? pageCount;
        firstPage = Math.Max(1, firstPage);
        lastPage = Math.Min(pageCount, lastPage);
        var result = new List<PdfTextLine>();
        var firstPageFound = -1;
        var lastPageFound = -1;
        for (var pageNum = firstPage; pageNum <= lastPage; pageNum++) {
            var pageLines = GetLines(pdfId, pageNum, yTolerance);
            var sectionStartIdx = -1;
            for (var i = 0; i < pageLines.Count; i++) {
                if (!pageLines[i].Text.Trim().StartsWith(startSection.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                sectionStartIdx = i;
                break;
            }

            if (sectionStartIdx < 0)
                continue;

            if (firstPageFound < 0)
                firstPageFound = pageNum;

            lastPageFound = pageNum;
            string? endSection = null;
            for (var i = sectionStartIdx + 1; i < pageLines.Count; i++) {
                var lineText = pageLines[i].Text.Trim();
                var found = endCandidates.FirstOrDefault(s => lineText.StartsWith(s, StringComparison.OrdinalIgnoreCase));
                if (found == null)
                    continue;

                endSection = found;
                break;
            }

            endSection ??= defaultEndSection;
            var sectionLines = GetLinesBetween(pdfId, startSection, endSection, pageNum, yTolerance);
            result.AddRange(sectionLines);
        }

        if (firstPageFound < 0)
            return null;

        return new(startSection.Trim(), firstPageFound, lastPageFound, result);
    }

    /// <inheritdoc />
    public async Task<PdfSection?> GetSectionAsync(
        Guid pdfId,
        string startSection,
        IEnumerable<string> sectionsInOrder,
        string? defaultEndSection = null,
        int? startPage = null,
        int? endPage = null,
        double? yTolerance = null,
        CancellationToken ct = default)
        => await Task.Run(() => GetSection(pdfId, startSection, sectionsInOrder, defaultEndSection, startPage, endPage, yTolerance), ct).ConfigureAwait(false);

    /// <inheritdoc />
    public DataTable.Models.DataTable ExtractDataTable(Guid pdfId, ColumnHeader[] headers, int? page = null, double yTolerance = 5.0)
    {
        ArgumentHelpers.ThrowIfNull(headers, nameof(headers));
        var (headerCells, formattedRows) = ExtractTableFormattedInternal(pdfId, headers, page, yTolerance);
        return RowsToDataTable(headers, formattedRows, headerCells);
    }

    /// <inheritdoc />
    public DataTable.Models.DataTable ExtractDataTable(IReadOnlyList<PdfWord> words, ColumnHeader[] headers, double yTolerance = 5.0)
    {
        ArgumentHelpers.ThrowIfNull(words, nameof(words));
        ArgumentHelpers.ThrowIfNull(headers, nameof(headers));
        var (headerCells, formattedRows) = ExtractTableFromWordsFormatted(words.ToList(), headers, yTolerance, inferFormattingForHeaderRows: null);
        return RowsToDataTable(headers, formattedRows, headerCells);
    }

    /// <inheritdoc />
    public async Task<DataTable.Models.DataTable> ExtractDataTableAsync(
        Guid pdfId,
        ColumnHeader[] headers,
        int? page = null,
        double yTolerance = 5.0,
        CancellationToken ct = default)
        => await Task.Run(() => ExtractDataTable(pdfId, headers, page, yTolerance), ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<DataTable.Models.DataTable> ExtractDataTableAsync(
        IReadOnlyList<PdfWord> words,
        ColumnHeader[] headers,
        double yTolerance = 5.0,
        CancellationToken ct = default)
        => await Task.Run(() => ExtractDataTable(words, headers, yTolerance), ct).ConfigureAwait(false);

    private static DataTable.Models.DataTable RowsToDataTable(ColumnHeader[] headers, IReadOnlyList<IReadOnlyDictionary<string, string?>> rows)
    {
        var dt = new DataTable.Models.DataTable();
        for (var i = 0; i < headers.Length; i++)
            dt.SetHeader(i, DataTableCell.FromValue(headers[i].Label));

        for (var r = 0; r < rows.Count; r++) {
            var row = rows[r];
            var dataRow = dt.AddRow();
            for (var c = 0; c < headers.Length; c++) {
                var val = row.TryGetValue(headers[c].Label, out var v) ? v : null;
                dataRow.SetCell(c, DataTableCell.FromValue(val ?? ""));
            }
        }

        return dt;
    }

    private static DataTable.Models.DataTable RowsToDataTable(
        ColumnHeader[] headers,
        IReadOnlyList<IReadOnlyDictionary<string, IDataTableCell>> formattedRows,
        IReadOnlyList<IDataTableCell>? formattedHeaderCells = null)
    {
        var dt = new DataTable.Models.DataTable();
        for (var i = 0; i < headers.Length; i++) {
            var headerCell = formattedHeaderCells != null && i < formattedHeaderCells.Count ? formattedHeaderCells[i] : DataTableCell.FromValue(headers[i].Label);
            dt.SetHeader(i, headerCell);
        }

        for (var r = 0; r < formattedRows.Count; r++) {
            var row = formattedRows[r];
            var dataRow = dt.AddRow();
            for (var c = 0; c < headers.Length; c++) {
                var cell = row.TryGetValue(headers[c].Label, out var v) ? v : DataTableCell.FromValue("");
                dataRow.SetCell(c, cell);
            }
        }

        return dt;
    }

    private async Task<Guid> LoadPdfFromFileIdAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(filePath, nameof(filePath));
#if NETSTANDARD2_0
        return await Task.Run(
                () => {
                    var bytes = File.ReadAllBytes(filePath);
                    return LoadPdfInternal(bytes, filePath);
                }, ct)
            .ConfigureAwait(false);
#else
        var bytes = await File.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);
        return LoadPdfInternal(bytes, filePath);
#endif
    }

    private Guid LoadPdfFromFileId(string filePath)
    {
        ArgumentHelpers.ThrowIfFileNotFound(filePath, nameof(filePath));
        var bytes = File.ReadAllBytes(filePath);
        return LoadPdfInternal(bytes, filePath);
    }

    private async Task<Guid> LoadPdfFromUrlIdAsync(string url, CancellationToken ct = default)
    {
        UriHelpers.ThrowIfInvalidUri(url, nameof(url));
        var client = _httpClient ?? new HttpClient();
        byte[] bytes;
        try {
#if NETSTANDARD2_0
            bytes = await client.GetByteArrayAsync(url).ConfigureAwait(false);
#else
            bytes = await client.GetByteArrayAsync(url, ct).ConfigureAwait(false);
#endif
        }
        finally {
            if (_httpClient == null)
                client.Dispose();
        }

        return LoadPdfInternal(bytes, url: url);
    }

    private Guid LoadPdfFromUrlId(string url) => LoadPdfFromUrlIdAsync(url).GetAwaiter().GetResult();

    private Task<Guid> LoadPdfFromBytesIdAsync(byte[] pdfBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(pdfBytes, nameof(pdfBytes));
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(LoadPdfInternal(pdfBytes));
    }

    private Guid LoadPdfFromBytesId(byte[] pdfBytes)
    {
        ArgumentHelpers.ThrowIfNull(pdfBytes, nameof(pdfBytes));
        return LoadPdfInternal(pdfBytes);
    }

    private async Task<Guid> LoadPdfFromStreamIdAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream, nameof(stream));
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        return await Task.Run(
                async () => {
                    using var memoryStream = new MemoryStream();
                    if (stream.CanSeek)
                        stream.Position = 0;

#if NETSTANDARD2_0
                    await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
#else
                    await stream.CopyToAsync(memoryStream, ct).ConfigureAwait(false);
#endif
                    return LoadPdfInternal(memoryStream.ToArray());
                }, ct)
            .ConfigureAwait(false);
    }

    private Guid LoadPdfFromStreamId(Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream, nameof(stream));
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        using var memoryStream = new MemoryStream();
        if (stream.CanSeek)
            stream.Position = 0;

        stream.CopyTo(memoryStream);
        return LoadPdfInternal(memoryStream.ToArray());
    }

    private Guid LoadPdfInternal(byte[] pdfBytes, string? filePath = null, string? url = null)
        => ExecuteWithMetrics(
            Constants.Metrics.LoadDuration, Constants.Metrics.LoadSuccess, Constants.Metrics.LoadFailure, () => {
                if (pdfBytes.Length > _maxPdfSizeBytes)
                    throw new ArgumentOutOfRangeException(nameof(pdfBytes), $"PDF size ({pdfBytes.Length} bytes) exceeds max allowed size ({_maxPdfSizeBytes} bytes).");

                _documentsLock.EnterWriteLock();
                try {
                    var projectedTotal = _totalLoadedBytes + pdfBytes.Length;
                    OperationHelpers.ThrowIf(
                        projectedTotal > _maxTotalLoadedBytes,
                        $"Loading this PDF would exceed the total loaded bytes limit ({_maxTotalLoadedBytes} bytes). Current total: {_totalLoadedBytes} bytes.");

                    var pdf = PdfDocument.Open(pdfBytes);
                    var id = Guid.NewGuid();
                    var loadedPdf = new LoadedPdf(id, pdf, pdfBytes, filePath, url);
                    _loadedDocuments[id] = loadedPdf;
                    _totalLoadedBytes = projectedTotal;
                    _logger.LogDebug("Loaded PDF with ID {PdfId}. Pages: {PageCount}, Source: {Source}", id, pdf.NumberOfPages, filePath ?? url ?? "bytes");
                    return id;
                }
                finally {
                    _documentsLock.ExitWriteLock();
                }
            });

    private void UnloadPdfInternal(Guid pdfId)
        => ExecuteWithMetrics(
            Constants.Metrics.UnloadDuration, Constants.Metrics.UnloadSuccess, Constants.Metrics.UnloadFailure, () => {
                _documentsLock.EnterWriteLock();
                try {
                    if (!_loadedDocuments.TryGetValue(pdfId, out var loadedPdf))
                        return;

                    loadedPdf.Document.Dispose();
                    _loadedDocuments.Remove(pdfId);
                    _totalLoadedBytes -= loadedPdf.OriginalBytes.Length;
                    _logger.LogDebug("Unloaded PDF with ID {PdfId}", pdfId);
                }
                finally {
                    _documentsLock.ExitWriteLock();
                }
            });

    private LoadedPdfLease CreateLease(Guid pdfId) => new(pdfId, UnloadPdf, id => UnloadPdfAsync(id));

    private T WithLoadedPdfRead<T>(Guid pdfId, Func<LoadedPdf, T> reader)
    {
        _documentsLock.EnterReadLock();
        try {
            OperationHelpers.ThrowIf(!_loadedDocuments.TryGetValue(pdfId, out var loadedPdf), $"PDF with ID {pdfId} is not loaded.");
            return reader(loadedPdf);
        }
        finally {
            _documentsLock.ExitReadLock();
        }
    }

    private void WithLoadedPdfRead(Guid pdfId, Action<LoadedPdf> reader)
    {
        _documentsLock.EnterReadLock();
        try {
            OperationHelpers.ThrowIf(!_loadedDocuments.TryGetValue(pdfId, out var loadedPdf), $"PDF with ID {pdfId} is not loaded.");
            reader(loadedPdf);
        }
        finally {
            _documentsLock.ExitReadLock();
        }
    }

    private async Task<IReadOnlyList<LoadedPdfLease>> LoadBatchLeasesAsync<T>(IReadOnlyList<T> inputs, Func<T, CancellationToken, Task<Guid>> loader, CancellationToken ct)
    {
        var loadedIds = new List<Guid>(inputs.Count);
        try {
            foreach (var input in inputs) {
                ct.ThrowIfCancellationRequested();
                loadedIds.Add(await loader(input, ct).ConfigureAwait(false));
            }
        }
        catch {
            UnloadLoadedPdfs(loadedIds);
            throw;
        }

        return loadedIds.Select(CreateLease).ToList();
    }

    private IReadOnlyList<LoadedPdfLease> LoadBatchLeases<T>(IReadOnlyList<T> inputs, Func<T, Guid> loader)
    {
        var loadedIds = new List<Guid>(inputs.Count);
        try {
            foreach (var input in inputs)
                loadedIds.Add(loader(input));
        }
        catch {
            UnloadLoadedPdfs(loadedIds);
            throw;
        }

        return loadedIds.Select(CreateLease).ToList();
    }

    private PdfInfo GetPdfInfoInternal(Guid pdfId)
        => ExecuteWithMetrics(
            Constants.Metrics.InfoDuration, Constants.Metrics.InfoSuccess, Constants.Metrics.InfoFailure, () => {
                return WithLoadedPdfRead<PdfInfo>(
                    pdfId, loadedPdf => {
                        var document = loadedPdf.Document;
                        var info = document.Information;
                        DateTime? creationDate = null;
                        DateTime? modifiedDate = null;
                        if (!string.IsNullOrWhiteSpace(info.CreationDate) && DateTime.TryParse(info.CreationDate, out var cd))
                            creationDate = cd;

                        if (!string.IsNullOrWhiteSpace(info.ModifiedDate) && DateTime.TryParse(info.ModifiedDate, out var md))
                            modifiedDate = md;

                        return new(
                            document.NumberOfPages, info.Title, info.Author, info.Subject, info.Creator, info.Producer, loadedPdf.FilePath, loadedPdf.Url, creationDate,
                            modifiedDate);
                    });
            });

    private IReadOnlyList<PdfWord> GetWordsInternal(Guid pdfId, int? page)
        => ExecuteWithMetrics(
            Constants.Metrics.WordsDuration, Constants.Metrics.WordsSuccess, Constants.Metrics.WordsFailure, () => WithLoadedPdfRead(
                pdfId, loadedPdf => {
                    var document = loadedPdf.Document;
                    if (page.HasValue) {
                        if (page.Value < 1 || page.Value > document.NumberOfPages)
                            throw new ArgumentOutOfRangeException(nameof(page), $"Page number must be between 1 and {document.NumberOfPages}.");

                        var pdfPage = document.GetPage(page.Value);
                        return pdfPage.GetWords().ToPdfWords(pdfPage.Paths);
                    }

                    var allWords = new List<PdfWord>();
                    for (var i = 1; i <= document.NumberOfPages; i++) {
                        var pdfPage = document.GetPage(i);
                        allWords.AddRange(pdfPage.GetWords().ToPdfWords(pdfPage.Paths));
                    }

                    return allWords;
                }));

    private IReadOnlyList<PdfTextLine> GetLinesInternal(Guid pdfId, int? page, double? yTolerance)
        => ExecuteWithMetrics(
            Constants.Metrics.LinesDuration, Constants.Metrics.LinesSuccess, Constants.Metrics.LinesFailure, () => WithLoadedPdfRead(
                pdfId, loadedPdf => {
                    var document = loadedPdf.Document;
                    var tolerance = yTolerance ?? _options.DefaultYTolerance;
                    if (page.HasValue) {
                        if (page.Value < 1 || page.Value > document.NumberOfPages)
                            throw new ArgumentOutOfRangeException(nameof(page), $"Page number must be between 1 and {document.NumberOfPages}.");

                        var pdfPage = document.GetPage(page.Value);
                        return GroupIntoLines(pdfPage.GetWords().ToPdfWords(pdfPage.Paths), tolerance);
                    }

                    var lines = new List<PdfTextLine>();
                    for (var i = 1; i <= document.NumberOfPages; i++) {
                        var pdfPage = document.GetPage(i);
                        lines.AddRange(GroupIntoLines(pdfPage.GetWords().ToPdfWords(pdfPage.Paths), tolerance).OrderByDescending(l => l.Y));
                    }

                    return lines;
                }));

    private List<PdfTextLine> GetDocumentLines(PdfDocument document, double yTolerance, int? page = null)
    {
        var lines = new List<PdfTextLine>();
        var startPage = page ?? 1;
        var endPage = page ?? document.NumberOfPages;
        if (page.HasValue && (page.Value < 1 || page.Value > document.NumberOfPages))
            throw new ArgumentOutOfRangeException(nameof(page), $"Page number must be between 1 and {document.NumberOfPages}.");

        for (var pageNumber = startPage; pageNumber <= endPage; pageNumber++) {
            var pdfPage = document.GetPage(pageNumber);
            lines.AddRange(GroupIntoLines(pdfPage.GetWords().ToPdfWords(pdfPage.Paths), yTolerance).OrderByDescending(l => l.Y));
        }

        return lines;
    }

    private IReadOnlyList<PdfWord> GetWordsBetweenInternal(Guid pdfId, string? startText, string? endText, int? page)
        => ExecuteWithMetrics(
            Constants.Metrics.WordsBetweenDuration, Constants.Metrics.WordsBetweenSuccess, Constants.Metrics.WordsBetweenFailure, () => {
                var tolerance = _options.DefaultYTolerance;
                var lines = WithLoadedPdfRead(pdfId, loadedPdf => GetDocumentLines(loadedPdf.Document, tolerance, page));
                var sectionLines = FindSectionLines(lines, startText, endText);
                return sectionLines.SelectMany(l => l.Words.OrderBy(w => w.BoundingBox.Left)).ToList();
            });

    private IReadOnlyList<PdfTextLine> GetLinesBetweenInternal(Guid pdfId, string? startText, string? endText, int? page, double? yTolerance)
        => ExecuteWithMetrics(
            Constants.Metrics.LinesBetweenDuration, Constants.Metrics.LinesBetweenSuccess, Constants.Metrics.LinesBetweenFailure, () => {
                var tolerance = yTolerance ?? _options.DefaultYTolerance;
                var lines = WithLoadedPdfRead(pdfId, loadedPdf => GetDocumentLines(loadedPdf.Document, tolerance, page));
                return FindSectionLines(lines, startText, endText);
            });

    private IReadOnlyList<PdfTextLine> GetLinesInBoundingBoxInternal(Guid pdfId, PdfBoundingBox region, double? yTolerance)
        => ExecuteWithMetrics(
            Constants.Metrics.LinesDuration, Constants.Metrics.LinesSuccess, Constants.Metrics.LinesFailure, () => WithLoadedPdfRead(
                pdfId, loadedPdf => {
                    var document = loadedPdf.Document;
                    if (region.Page < 1 || region.Page > document.NumberOfPages)
                        throw new ArgumentOutOfRangeException(nameof(region), $"Page number must be between 1 and {document.NumberOfPages}.");

                    var pdfPage = document.GetPage(region.Page);
                    var overlapThreshold = Math.Max(0, Math.Min(1, _options.BoundingBoxOverlapThreshold));

                    // Page-content words: apply overlap threshold (excludes words that barely touch the region)
                    var pageWords = pdfPage.GetWords().ToPdfWords(pdfPage.Paths).ToList();
                    var filteredPageWords = pageWords.Where(w => w.BoundingBox.OverlapRatio(region.Box) >= overlapThreshold).ToList();

                    // Annotation text: use Intersects only (form fields have large rects; user's box may be smaller and fully inside)
                    var words = new List<PdfWord>(filteredPageWords);
                    foreach (var ann in pdfPage.GetAnnotations()) {
                        var text = GetAnnotationText(ann);
                        if (string.IsNullOrWhiteSpace(text))
                            continue;

                        var rect = ann.Rectangle;
                        var annBox = new BoundingBox2D(rect.Left, rect.Right, rect.Top, rect.Bottom);
                        if (!annBox.Intersects(region.Box))
                            continue;

                        var format = GetAnnotationFormat(ann);
                        words.Add(new(text!.Trim(), annBox, format));
                    }

                    var tolerance = yTolerance ?? _options.DefaultYTolerance;
                    words = DedupeOverlappingDuplicateText(words);
                    return GroupIntoLines(words, tolerance);
                }));

    /// <summary>When the same text appears twice (drawn text + AcroForm widget), keep the tighter bounding box and drop the duplicate.</summary>
    private static List<PdfWord> DedupeOverlappingDuplicateText(IReadOnlyList<PdfWord> words)
    {
        var candidates = words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).OrderBy(w => w.BoundingBox.Width * w.BoundingBox.Height).ToList();
        if (candidates.Count <= 1)
            return candidates;

        var kept = new List<PdfWord>(candidates.Count);
        foreach (var w in candidates) {
            var tw = DedupeNormalizeText(w.Text);
            if (tw.Length == 0) {
                kept.Add(w);
                continue;
            }

            var duplicate = false;
            foreach (var k in kept) {
                if (!string.Equals(DedupeNormalizeText(k.Text), tw, StringComparison.OrdinalIgnoreCase))
                    continue;

                var o1 = k.BoundingBox.OverlapRatio(w.BoundingBox);
                var o2 = w.BoundingBox.OverlapRatio(k.BoundingBox);
                if (o1 > 0.35 || o2 > 0.35) {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
                kept.Add(w);
        }

        return kept;
    }

    private static string DedupeNormalizeText(string text) => text.Trim().Replace('\u00a0', ' ');

    private Dictionary<string, string?> InferKeyValuePairsFromFormattingInternal(List<PdfWord> words, double yTolerance, PdfInferFormattingFlags inferFlags)
    {
        var tolerance = yTolerance > 0 ? yTolerance : _options.DefaultYTolerance;
        var lines = GroupIntoLines(words, tolerance).OrderByDescending(l => l.Y).ToList();
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (inferFlags == PdfInferFormattingFlags.None)
            return result;

        var useDelimiters = (inferFlags & PdfInferFormattingFlags.Semicolon) != 0;
        var useFontEmphasis = (inferFlags & (PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline)) != 0;

        string? pendingKey = null;
        var valueParts = new List<string>();

        void FlushPending()
        {
            if (string.IsNullOrWhiteSpace(pendingKey))
                return;

            var key = CanonicalInferredKey(pendingKey!);
            pendingKey = null;
            var val = string.Join(" ", valueParts).Trim();
            valueParts.Clear();
            if (string.IsNullOrEmpty(key))
                return;

            if (result.TryGetValue(key, out var existing) && !string.IsNullOrWhiteSpace(existing))
                result[key] = string.IsNullOrWhiteSpace(val) ? existing : existing + " " + val;
            else
                result[key] = string.IsNullOrWhiteSpace(val) ? null : val;
        }

        foreach (var line in lines) {
            var ws = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();
            if (ws.Count == 0)
                continue;

            var lineText = string.Join(" ", ws.Select(w => w.Text)).Trim();
            if (useDelimiters && TryParseDelimiterKeyValueLine(lineText, out var delimKey, out var delimVal, out var delimLabelOnly)) {
                FlushPending();
                if (delimLabelOnly) {
                    pendingKey = delimKey;
                    continue;
                }

                var ckey = CanonicalInferredKey(delimKey!);
                if (!string.IsNullOrEmpty(ckey)) {
                    if (result.TryGetValue(ckey, out var ex) && !string.IsNullOrWhiteSpace(ex))
                        result[ckey] = string.IsNullOrWhiteSpace(delimVal) ? ex : ex + " " + delimVal;
                    else
                        result[ckey] = string.IsNullOrWhiteSpace(delimVal) ? null : delimVal;
                }

                continue;
            }

            if (!useFontEmphasis) {
                if (pendingKey != null)
                    valueParts.Add(lineText);

                continue;
            }

            var allE = ws.All(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags));
            var anyE = ws.Any(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags));

            if (allE) {
                if (LooksLikeSectionHeader(lineText)) {
                    FlushPending();
                    continue;
                }

                FlushPending();
                pendingKey = lineText;
                continue;
            }

            if (anyE && !allE) {
                FlushPending();
                var i = 0;
                while (i < ws.Count && PdfFontStyleInference.IsInferEmphasizedForFlags(ws[i], inferFlags))
                    i++;

                if (i == 0) {
                    if (pendingKey != null)
                        valueParts.Add(lineText);

                    continue;
                }

                var keyText = string.Join(" ", ws.Take(i).Select(w => w.Text)).Trim();
                var rest = string.Join(" ", ws.Skip(i).Select(w => w.Text)).Trim();
                var ck = CanonicalInferredKey(keyText);
                if (!string.IsNullOrEmpty(ck)) {
                    if (result.TryGetValue(ck, out var ex) && !string.IsNullOrWhiteSpace(ex))
                        result[ck] = string.IsNullOrWhiteSpace(rest) ? ex : ex + " " + rest;
                    else
                        result[ck] = string.IsNullOrWhiteSpace(rest) ? null : rest;
                }

                continue;
            }

            if (pendingKey != null) {
                valueParts.Add(lineText);
            }
        }

        FlushPending();
        return result;
    }

    /// <summary>Bounds key/value vertical band count (netstandard2.0 has no Math.Clamp).</summary>
    private static int ClampKvColumnBandCount(int value) => value < 1 ? 1 : (value > 32 ? 32 : value);

    /// <summary>Parses colon- or semicolon-terminated key/value lines (standalone label or inline value).</summary>
    private static bool TryParseDelimiterKeyValueLine(string lineText, out string? key, out string? value, out bool labelOnly)
    {
        key = null;
        value = null;
        labelOnly = false;
        var t = lineText.Trim();
        if (string.IsNullOrEmpty(t))
            return false;

        if (TryParsePunctuationKeyValueLine(t, ':', colonUrlGuard: true, out key, out value, out labelOnly))
            return true;

        return TryParsePunctuationKeyValueLine(t, ';', colonUrlGuard: false, out key, out value, out labelOnly);
    }

    /// <param name="colonUrlGuard">When the delimiter is colon, skip lines that look like URLs.</param>
    private static bool TryParsePunctuationKeyValueLine(
        string t,
        char delimiter,
        bool colonUrlGuard,
        out string? key,
        out string? value,
        out bool labelOnly)
    {
        key = null;
        value = null;
        labelOnly = false;
        if (t.IndexOf(delimiter) < 0)
            return false;

        if (colonUrlGuard) {
            if (t.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return false;

            if (t.IndexOf("://", StringComparison.Ordinal) >= 0 && t.IndexOf(": ", StringComparison.Ordinal) < 0)
                return false;
        }

        var delimStr = delimiter.ToString();
        var spaced = delimStr + " ";

        var trimmedEnd = t.TrimEnd();
        if (trimmedEnd.EndsWith(delimStr, StringComparison.Ordinal)) {
            var k = (trimmedEnd.Length <= 1 ? string.Empty : trimmedEnd.Substring(0, trimmedEnd.Length - 1)).Trim();
            if (!string.IsNullOrEmpty(k) && LooksPlausibleDelimiterKeyLabel(k)) {
                key = k;
                value = null;
                labelOnly = true;
                return true;
            }
        }

        var sp = t.Split(new[] { spaced }, 2, StringSplitOptions.None);
        if (sp.Length == 2) {
            var k = sp[0].Trim();
            var v = sp[1].Trim();
            if (!string.IsNullOrEmpty(k) && LooksPlausibleDelimiterKeyLabel(k)) {
                key = k;
                value = v;
                labelOnly = false;
                return true;
            }
        }

        var i = t.IndexOf(delimiter);
        if (i > 0 && i < t.Length - 1) {
            var k = t.Substring(0, i).Trim();
            var v = t.Substring(i + 1).Trim();
            if (!string.IsNullOrEmpty(k) && !string.IsNullOrEmpty(v) && LooksPlausibleDelimiterKeyLabel(k)) {
                key = k;
                value = v;
                labelOnly = false;
                return true;
            }
        }

        return false;
    }

    /// <summary>Reduces false positives from times (e.g. <c>12:30 PM</c>) while allowing short labels like <c>ID</c>.</summary>
    private static bool LooksPlausibleDelimiterKeyLabel(string k) => k.Any(char.IsLetter);

    private static string CanonicalInferredKey(string key) => key.Trim().TrimEnd(':', ';').Trim();

    /// <summary>Whether a line still looks like a styled header row (not body text) for the active inference flags.</summary>
    private static bool LineLooksLikeInferenceHeaderRow(PdfTextLine line, PdfInferFormattingFlags inferFlags)
    {
        if (line.Words.Count == 0)
            return false;

        var emphasized = line.Words.Count(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags));
        return emphasized / (double)line.Words.Count >= 0.28;
    }

    private static double InferenceEmphasisRatio(PdfTextLine line, PdfInferFormattingFlags inferFlags)
    {
        if (line.Words.Count == 0)
            return 0;

        var n = line.Words.Count(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags));
        return n / (double)line.Words.Count;
    }

    /// <summary>
    /// Extends a header block with <paramref name="nextLine"/> when it reads as the next row of the same styled header
    /// (multi-line underline), including rows where PDF emphasis is weaker than <see cref="LineLooksLikeInferenceHeaderRow"/>.
    /// </summary>
    private static bool LineQualifiesForHeaderBlockExtension(PdfTextLine? lineAbove, PdfTextLine nextLine, PdfInferFormattingFlags inferFlags)
    {
        if (nextLine.Words.Count == 0)
            return false;

        if (LineLooksLikeInferenceHeaderRow(nextLine, inferFlags))
            return true;

        if ((inferFlags & (PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline)) == 0)
            return false;

        var nextR = InferenceEmphasisRatio(nextLine, inferFlags);
        if (nextR < 0.07)
            return false;

        if (lineAbove != null) {
            var aboveR = InferenceEmphasisRatio(lineAbove, inferFlags);
            if (aboveR >= 0.20 && nextR >= 0.08)
                return true;
        }

        return nextLine.Words.Any(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags)) && nextR >= 0.10;
    }

    /// <summary>
    /// When inferring from bold/underline, lines with almost no matching emphasis are unlikely to be the primary header row
    /// (they may be body text or a title without the same styling).
    /// </summary>
    private static bool LineHasNegligibleInferenceEmphasis(PdfTextLine line, PdfInferFormattingFlags inferFlags)
    {
        if (line.Words.Count == 0)
            return true;

        if ((inferFlags & (PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline)) == 0)
            return false;

        var ratio = line.Words.Count(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags)) / (double)line.Words.Count;
        return ratio < 0.08;
    }

    /// <summary>Splits a header line on <c>; </c> or <c>: </c> when that yields multiple cells.</summary>
    private static string[] SplitHeaderLineByDelimiters(string joined)
    {
        var t = joined.Trim();
        if (t.Length == 0)
            return [];

        if (t.IndexOf("; ", StringComparison.Ordinal) >= 0) {
            var parts = t.Split(new[] { "; " }, StringSplitOptions.None).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            if (parts.Length >= 2)
                return parts;
        }

        if (t.IndexOf("://", StringComparison.Ordinal) < 0 && t.IndexOf(": ", StringComparison.Ordinal) >= 0) {
            var parts = t.Split(new[] { ": " }, StringSplitOptions.None).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            if (parts.Length >= 2)
                return parts;
        }

        return [];
    }

    private ColumnHeader[] InferTableHeadersFromFormattingInternal(List<PdfWord> words, double? yTolerance, PdfInferFormattingFlags inferFlags)
    {
        if (inferFlags == PdfInferFormattingFlags.None)
            return [];

        var tolerance = yTolerance ?? _options.DefaultYTolerance;
        var lines = GroupIntoLines(words, tolerance).OrderByDescending(l => l.Y).ToList();
        if (lines.Count == 0)
            return [];

        var useFontEmphasis = (inferFlags & (PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline)) != 0;
        var useDelimiters = (inferFlags & (PdfInferFormattingFlags.Semicolon)) != 0;

        var bestIdx = 0;
        PdfTextLine? bestLine = null;
        if (useFontEmphasis) {
            var scan = Math.Min(8, lines.Count);
            var scored = new List<(int Idx, double Score)>();
            for (var li = 0; li < scan; li++) {
                var line = lines[li];
                var ws = line.Words;
                if (ws.Count < 2)
                    continue;

                // Skip lines that don't match the selected inference styling (e.g. plain body rows when inferring underline).
                if (LineHasNegligibleInferenceEmphasis(line, inferFlags))
                    continue;

                var emphasized = ws.Count(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags));
                var ratio = (double)emphasized / ws.Count;
                var avgSize = ws.Where(w => w.Format?.FontSize is > 0).Select(w => w.Format!.FontSize!.Value).DefaultIfEmpty(0).Average();
                var score = ratio * 3.0 + avgSize * 0.05;
                scored.Add((li, score));
            }

            if (scored.Count > 0) {
                var maxScore = scored.Max(s => s.Score);
                // Among similar scores, prefer the topmost line (smallest index — lines are sorted by descending Y).
                const double tieEps = 0.08;
                var best = scored.Where(s => s.Score >= maxScore - tieEps).OrderBy(s => s.Idx).First();
                bestIdx = best.Idx;
                bestLine = lines[bestIdx];
            }
        }

        if (bestLine == null) {
            bestLine = lines[0];
            bestIdx = 0;
        }

        // Lookahead: include consecutive lines while each still looks like part of the styled header (spacing + formatting).
        var mergeTh = _options.TableHeaderMergeThreshold;
        var blockIndices = new List<int> { bestIdx };
        var i = bestIdx;
        while (i + 1 < lines.Count
            && lines[i].Y - lines[i + 1].Y <= mergeTh
            && LineQualifiesForHeaderBlockExtension(lines[i], lines[i + 1], inferFlags)) {
            i++;
            blockIndices.Add(i);
        }

        i = bestIdx;
        while (i > 0
            && lines[i - 1].Y - lines[i].Y <= mergeTh
            && LineQualifiesForHeaderBlockExtension(lines[i - 1], lines[i], inferFlags)) {
            i--;
            blockIndices.Insert(0, i);
        }

        blockIndices.Sort();
        var blockLines = blockIndices.Select(idx => lines[idx]).ToList();

        var joinedBlock = string.Join(" ", blockLines.Select(l => string.Join(" ", l.Words.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)))).Trim();
        var joinedBestSingle = string.Join(" ", bestLine.Words.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)).Trim();

        if (useDelimiters) {
            var split = SplitHeaderLineByDelimiters(joinedBlock);
            if (split.Length >= 2)
                return split.Select(s => new ColumnHeader(s)).ToArray();

            split = SplitHeaderLineByDelimiters(joinedBestSingle);
            if (split.Length >= 2)
                return split.Select(s => new ColumnHeader(s)).ToArray();
        }

        if (useFontEmphasis)
            return InferColumnHeadersFromEmphasizedBlock(blockLines);

        return [];
    }

    /// <summary>Infers one column header per horizontal band by clustering words using X gaps (not every word is a column).</summary>
    private ColumnHeader[] InferColumnHeadersFromEmphasizedBlock(IReadOnlyList<PdfTextLine> blockLines)
    {
        if (blockLines.Count == 0)
            return [];

        var anchor = blockLines.OrderByDescending(l => l.Words.Count).First();
        var xTol = _options.TableColumnXTolerance;
        var ordered = anchor.Words.OrderBy(w => w.BoundingBox.Left).ToList();
        if (ordered.Count == 0)
            return [];

        var anchorCols = ClusterWordsIntoHeaderColumns(ordered, xTol, blockLines.Count);
        if (anchorCols.Count == 0)
            return [];

        // Single header row: column boundaries come from horizontal spacing only.
        if (blockLines.Count == 1)
            return BuildColumnHeadersFromGapClusters(anchorCols);

        // Multi-line header: if the anchor line didn't split into columns, try the top physical line.
        if (anchorCols.Count == 1 && blockLines[0].Words.Count > 0) {
            var topOrdered = blockLines[0].Words.OrderBy(w => w.BoundingBox.Left).ToList();
            var topCols = ClusterWordsIntoHeaderColumns(topOrdered, xTol, 1);
            if (topCols.Count > 1)
                anchorCols = topCols;
        }

        if (anchorCols.Count == 1) {
            var joined = string.Join(" ", anchorCols[0].Select(w => w.Text.Trim()).Where(t => !string.IsNullOrWhiteSpace(t))).Trim();
            return string.IsNullOrWhiteSpace(joined) ? [] : [new ColumnHeader(joined)];
        }

        var columnRanges = anchorCols.Select(col => {
            var left = col.Min(w => w.BoundingBox.Left) - xTol;
            var right = col.Max(w => w.BoundingBox.Right) + xTol;
            return (Left: left, Right: right);
        }).ToList();

        var buckets = Enumerable.Range(0, columnRanges.Count).Select(_ => new List<string>()).ToArray();
        foreach (var line in blockLines) {
            foreach (var w in line.Words.OrderBy(x => x.BoundingBox.Left)) {
                var j = AssignWordToColumnIndex(w, columnRanges);
                var t = w.Text.Trim();
                if (!string.IsNullOrWhiteSpace(t))
                    buckets[j].Add(t);
            }
        }

        var headers = new List<ColumnHeader>();
        foreach (var b in buckets) {
            if (b.Count == 0)
                continue;

            var label = string.Join(" ", b);
            if (!string.IsNullOrWhiteSpace(label))
                headers.Add(new ColumnHeader(label));
        }

        return headers.Count > 0 ? headers.ToArray() : [];
    }

    private List<List<PdfWord>> ClusterWordsIntoHeaderColumns(IReadOnlyList<PdfWord> ordered, double xTol, int blockLineCount)
    {
        var words = ordered as List<PdfWord> ?? ordered.ToList();
        var minGutter = ComputeAdaptiveColumnGutter(ordered, xTol);
        var cols = SplitWordsIntoColumnsByHorizontalGaps(words, minGutter);
        if (cols.Count == 1 && ordered.Count >= 3) {
            minGutter = Math.Max(4.0, minGutter * 0.35);
            cols = SplitWordsIntoColumnsByHorizontalGaps(words, minGutter);
        }

        if (cols.Count == 1 && ordered.Count >= 4 && blockLineCount > 1) {
            minGutter = Math.Max(3.0, minGutter * 0.28);
            cols = SplitWordsIntoColumnsByHorizontalGaps(words, minGutter);
        }

        return cols;
    }

    private static ColumnHeader[] BuildColumnHeadersFromGapClusters(List<List<PdfWord>> clusters)
    {
        var labels = clusters
            .Select(col => string.Join(" ", col.Select(w => w.Text.Trim()).Where(t => !string.IsNullOrWhiteSpace(t))).Trim())
            .Where(l => l.Length > 0)
            .ToList();
        labels = MergeOrphanDtHeaderTokens(labels);
        return labels.Count == 0 ? [] : labels.Select(l => new ColumnHeader(l)).ToArray();
    }

    private static double ComputeAdaptiveColumnGutter(IReadOnlyList<PdfWord> wordsSortedLeftToRight, double toleranceFallback)
    {
        if (wordsSortedLeftToRight.Count < 2)
            return Math.Max(12.0, toleranceFallback * 2.0);

        var gaps = new List<double>();
        for (var i = 1; i < wordsSortedLeftToRight.Count; i++) {
            var g = wordsSortedLeftToRight[i].BoundingBox.Left - wordsSortedLeftToRight[i - 1].BoundingBox.Right;
            if (g > 0.5)
                gaps.Add(g);
        }

        if (gaps.Count == 0)
            return Math.Max(12.0, toleranceFallback * 2.0);

        gaps.Sort();
        var median = gaps[gaps.Count / 2];
        return Math.Max(8.0, Math.Min(median * 2.5, toleranceFallback * 4.0));
    }

    private static List<List<PdfWord>> SplitWordsIntoColumnsByHorizontalGaps(IReadOnlyList<PdfWord> sortedLeftToRight, double minGutter)
    {
        if (sortedLeftToRight.Count == 0)
            return [];

        var cols = new List<List<PdfWord>>();
        var cur = new List<PdfWord> { sortedLeftToRight[0] };
        for (var i = 1; i < sortedLeftToRight.Count; i++) {
            var prev = sortedLeftToRight[i - 1];
            var w = sortedLeftToRight[i];
            var gap = w.BoundingBox.Left - prev.BoundingBox.Right;
            if (gap > minGutter) {
                cols.Add(cur);
                cur = [w];
            }
            else {
                cur.Add(w);
            }
        }

        cols.Add(cur);
        return cols;
    }

    private static int AssignWordToColumnIndex(PdfWord w, IReadOnlyList<(double Left, double Right)> columnRanges)
    {
        var cx = (w.BoundingBox.Left + w.BoundingBox.Right) * 0.5;
        for (var j = 0; j < columnRanges.Count; j++) {
            var r = columnRanges[j];
            if (cx >= r.Left && cx <= r.Right)
                return j;
        }

        var best = 0;
        var bestD = double.MaxValue;
        for (var j = 0; j < columnRanges.Count; j++) {
            var r = columnRanges[j];
            var center = (r.Left + r.Right) * 0.5;
            var d = Math.Abs(center - cx);
            if (d < bestD) {
                bestD = d;
                best = j;
            }
        }

        return best;
    }

    /// <summary>Gets text from an annotation: Content, /Contents, or /V (form field value). Handles FT=/Tx (text), FT=/Btn (checkbox).</summary>
    private static string? GetAnnotationText(Annotation ann)
    {
        if (!string.IsNullOrWhiteSpace(ann.Content))
            return ann.Content;

        var dict = ann.AnnotationDictionary;

        // /Contents - used by FreeText, Text, and other annotation types
        if (dict.TryGet(NameToken.Contents, out var contentsToken) && contentsToken is StringToken contentsSt && !string.IsNullOrWhiteSpace(contentsSt.Data))
            return contentsSt.Data;

        // /FT - field type: /Tx (text), /Btn (button/checkbox), /Ch (choice)
        var ft = GetTokenString(dict, "FT");

        // /Btn (checkbox): V = /1 or /Yes = checked, /Off = unchecked. Format: [x] Label or [] Label
        if (string.Equals(ft, "Btn", StringComparison.OrdinalIgnoreCase)) {
            var label = GetCheckboxLabel(dict);
            var vStr = GetTokenString(dict, "V");
            var isChecked = !string.IsNullOrEmpty(vStr) && !string.Equals(vStr, "Off", StringComparison.OrdinalIgnoreCase);
            var checkText = isChecked ? "[x]" : "[]";
            return string.IsNullOrWhiteSpace(label) ? checkText : $"{checkText} {label}";
        }

        // /Tx (text input): V holds the string value
        if (dict.TryGet(NameToken.Create("V"), out var valueToken)) {
            if (valueToken is StringToken valueSt && !string.IsNullOrWhiteSpace(valueSt.Data))
                return valueSt.Data;

            if (valueToken is HexToken valueHex && !string.IsNullOrWhiteSpace(valueHex.Data))
                return valueHex.Data;
        }

        return null;
    }

    /// <summary>Gets checkbox label: TU (tooltip) preferred; else T with internal names like c1_1[0] stripped.</summary>
    private static string? GetCheckboxLabel(DictionaryToken dict)
    {
        var tu = GetTokenString(dict, "TU");
        if (!string.IsNullOrWhiteSpace(tu))
            return tu!.Trim();

        var t = GetTokenString(dict, "T");
        if (string.IsNullOrWhiteSpace(t))
            return null;

        // Strip internal field names like "c1_1[0]", "f1_08[0]" from the start
        var stripped = Regex.Replace(t!.Trim(), @"^[\w\-]+\s*\[\d+\]\s*", "", RegexOptions.IgnoreCase);
        return string.IsNullOrWhiteSpace(stripped) ? null : stripped.Trim();
    }

    /// <summary>Gets string from dict key: StringToken, HexToken, or NameToken.</summary>
    private static string? GetTokenString(DictionaryToken dict, string key)
    {
        if (!dict.TryGet(NameToken.Create(key), out var token))
            return null;

        return token switch {
            StringToken st => st.Data,
            HexToken ht => ht.Data,
            NameToken nt => nt.Data,
            var _ => null
        };
    }

    /// <summary>Parses /DA (Default Appearance) to extract font, size, and color. Format: /Font size Tf [gray g | R G B rg].</summary>
    private static PdfWordFormat? GetAnnotationFormat(Annotation ann)
    {
        var da = GetDAString(ann);
        if (string.IsNullOrWhiteSpace(da))
            return null;

        string? fontName = null;
        double? fontSize = null;
        string? fontColor = null;
        var bold = false;
        var italic = false;

        // /FontName size Tf or (FontName) size Tf - font and size
        var tfMatch = Regex.Match(da, @"(?:/\s*([^\s/]+)|\(([^)]+)\))\s+([\d.]+)\s+Tf", RegexOptions.IgnoreCase);
        if (tfMatch.Success) {
            fontName = tfMatch.Groups[1].Success ? tfMatch.Groups[1].Value.Trim() : tfMatch.Groups[2].Value.Trim();
            if (double.TryParse(tfMatch.Groups[3].Value, out var size) && size > 0)
                fontSize = size;

            bold = PdfFontStyleInference.InferBold(fontName, bold);
            italic = PdfFontStyleInference.InferItalic(fontName, italic);
        }

        // R G B rg - RGB color
        var rgMatch = Regex.Match(da, @"([\d.]+)\s+([\d.]+)\s+([\d.]+)\s+rg", RegexOptions.IgnoreCase);
        if (rgMatch.Success && double.TryParse(rgMatch.Groups[1].Value, out var r) && double.TryParse(rgMatch.Groups[2].Value, out var g) &&
            double.TryParse(rgMatch.Groups[3].Value, out var b)) {
            var rr = (byte)(Math.Max(0, Math.Min(1, r)) * 255);
            var gg = (byte)(Math.Max(0, Math.Min(1, g)) * 255);
            var bb = (byte)(Math.Max(0, Math.Min(1, b)) * 255);
            fontColor = $"#{rr:X2}{gg:X2}{bb:X2}";
        }
        else {
            // gray g - grayscale
            var gMatch = Regex.Match(da, @"([\d.]+)\s+g\b", RegexOptions.IgnoreCase);
            if (gMatch.Success && double.TryParse(gMatch.Groups[1].Value, out var gray)) {
                var v = (byte)(Math.Max(0, Math.Min(1, gray)) * 255);
                fontColor = $"#{v:X2}{v:X2}{v:X2}";
            }
        }

        if (fontName == null && fontSize == null && fontColor == null && !bold && !italic)
            return null;

        return new(fontSize, fontName, bold, italic, fontColor);
    }

    private static string? GetDAString(Annotation ann)
    {
        var dict = ann.AnnotationDictionary;
        if (dict.TryGet(NameToken.Create("DA"), out var daToken) && daToken is StringToken daSt && !string.IsNullOrWhiteSpace(daSt.Data))
            return daSt.Data;

        return null;
    }

    private static List<PdfTextLine> FindSectionLines(List<PdfTextLine> lines, string? startText, string? endText)
    {
        if (lines.Count == 0)
            return [];

        var startIdx = 0;
        if (!string.IsNullOrWhiteSpace(startText)) {
            var normalizedStart = startText!.Trim();
            startIdx = lines.FindIndex(l => l.Text.Trim().StartsWith(normalizedStart, StringComparison.OrdinalIgnoreCase));
            if (startIdx < 0)
                return [];
        }

        var endIdx = lines.Count;
        if (!string.IsNullOrWhiteSpace(endText)) {
            var normalizedEnd = endText!.Trim();
            var found = lines.FindIndex(startIdx + 1, l => l.Text.Trim().IndexOf(normalizedEnd, StringComparison.OrdinalIgnoreCase) >= 0);
            if (found >= 0)
                endIdx = found;
        }

        return lines.Skip(startIdx).Take(Math.Max(0, endIdx - startIdx)).ToList();
    }

    private IReadOnlyList<KvColumnResult> ExtractKeyValuePairsInternal(
        Guid pdfId,
        IEnumerable<string> knownKeys,
        int? page,
        double yTolerance,
        PdfKeyValueLayout keyValueLayout,
        int keyValueColumnCount)
        => ExecuteWithMetrics(
            Constants.Metrics.ExtractKeyValueDuration, Constants.Metrics.ExtractKeyValueSuccess, Constants.Metrics.ExtractKeyValueFailure, () => {
                ArgumentHelpers.ThrowIfNull(knownKeys, nameof(knownKeys));
                var words = GetWordsInternal(pdfId, page);
                return ExtractKeyValueColumns(words.ToList(), knownKeys, yTolerance, keyValueLayout, keyValueColumnCount);
            });

    private (IReadOnlyList<IDataTableCell> HeaderCells, IReadOnlyList<IReadOnlyDictionary<string, IDataTableCell>> Rows) ExtractTableFormattedInternal(
        Guid pdfId,
        ColumnHeader[] headers,
        int? page,
        double yTolerance)
        => ExecuteWithMetrics(
            Constants.Metrics.ExtractTableDuration, Constants.Metrics.ExtractTableSuccess, Constants.Metrics.ExtractTableFailure, () => {
                ArgumentHelpers.ThrowIfNull(headers, nameof(headers));
                return WithLoadedPdfRead(
                    pdfId, loadedPdf => {
                        var document = loadedPdf.Document;
                        if (page.HasValue) {
                            if (page.Value < 1 || page.Value > document.NumberOfPages)
                                throw new ArgumentOutOfRangeException(nameof(page), $"Page number must be between 1 and {document.NumberOfPages}.");

                            var pdfPage = document.GetPage(page.Value);
                            return ExtractTableFromWordsFormatted(pdfPage.GetWords().ToPdfWords(pdfPage.Paths), headers, yTolerance);
                        }

                        var allWords = new List<PdfWord>();
                        for (var i = 1; i <= document.NumberOfPages; i++) {
                            var pdfPage = document.GetPage(i);
                            allWords.AddRange(pdfPage.GetWords().ToPdfWords(pdfPage.Paths));
                        }

                        return ExtractTableFromWordsFormatted(allWords, headers, yTolerance);
                    });
            });

    private IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTableInternal(Guid pdfId, ColumnHeader[] headers, int? page, double yTolerance)
    {
        var (_, rows) = ExtractTableFormattedInternal(pdfId, headers, page, yTolerance);
        return rows.Select(r => (IReadOnlyDictionary<string, string?>)r.ToDictionary(kv => kv.Key, kv => kv.Value.DisplayValue, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    private byte[] MergePdfsInternal(IEnumerable<Guid> pdfIds)
        => ExecuteWithMetrics(
            Constants.Metrics.MergeDuration, Constants.Metrics.MergeSuccess, Constants.Metrics.MergeFailure, () => {
                ArgumentHelpers.ThrowIfNull(pdfIds, nameof(pdfIds));
                var idsList = pdfIds.ToList();
                ArgumentHelpers.ThrowIfNullOrEmpty(idsList, nameof(idsList));
                ArgumentHelpers.ThrowIf(idsList.Count < 2, "At least two PDFs are required to merge.", nameof(pdfIds));
                using var outputDocument = new PdfSharp.Pdf.PdfDocument();
                foreach (var id in idsList) {
                    WithLoadedPdfRead(
                        id, loadedPdf => {
                            using var inputStream = new MemoryStream(loadedPdf.OriginalBytes, false);
                            using var sourceDocument = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
                            for (var pageIndex = 0; pageIndex < sourceDocument.PageCount; pageIndex++)
                                outputDocument.AddPage(sourceDocument.Pages[pageIndex]);
                        });
                }

                using var outputStream = new MemoryStream();
                outputDocument.Save(outputStream);
                return outputStream.ToArray();
            });

    private void SavePdfInternal(Guid pdfId, string filePath)
        => ExecuteWithMetrics(
            Constants.Metrics.SaveDuration, Constants.Metrics.SaveSuccess, Constants.Metrics.SaveFailure, () => {
                ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
                var bytes = GetPdfBytesInternal(pdfId);
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(filePath, bytes);
                _logger.LogDebug("Saved PDF with ID {PdfId} to {FilePath}", pdfId, filePath);
            });

    private byte[] GetPdfBytesInternal(Guid pdfId)
        => ExecuteWithMetrics(
            Constants.Metrics.BytesDuration, Constants.Metrics.BytesSuccess, Constants.Metrics.BytesFailure, () => WithLoadedPdfRead(pdfId, loadedPdf => loadedPdf.OriginalBytes));

    private static List<string> BuildMergePathInputs(string initialPdf, IEnumerable<string> paths)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(initialPdf, nameof(initialPdf));
        ArgumentHelpers.ThrowIfNull(paths, nameof(paths));
        var normalized = new List<string> { initialPdf };
        foreach (var path in paths) {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(paths));
            normalized.Add(path);
        }

        ArgumentHelpers.ThrowIf(normalized.Count < 2, nameof(paths), "At least two PDFs are required to merge.");
        return normalized;
    }

    private static List<byte[]> BuildMergeByteInputs(byte[] initialPdf, IEnumerable<byte[]> pdfs)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(initialPdf, nameof(initialPdf));
        ArgumentHelpers.ThrowIfNull(pdfs, nameof(pdfs));
        var normalized = new List<byte[]> { initialPdf };
        foreach (var pdf in pdfs) {
            ArgumentHelpers.ThrowIfNullOrEmpty(pdf, nameof(pdfs));
            normalized.Add(pdf);
        }

        ArgumentHelpers.ThrowIf(normalized.Count < 2, nameof(pdfs), "At least two PDFs are required to merge.");
        return normalized;
    }

    private void UnloadLoadedPdfs(IEnumerable<Guid> ids)
    {
        foreach (var id in ids) {
            try {
                UnloadPdfInternal(id);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to unload temporary PDF during merge cleanup. PDF ID: {PdfId}", id);
            }
        }
    }

    private T ExecuteWithMetrics<T>(string durationMetric, string successMetric, string failureMetric, Func<T> operation)
    {
        using var timer = _metrics.StartTimer(durationMetric);
        try {
            var result = operation();
            _metrics.IncrementCounter(successMetric);
            return result;
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(failureMetric);
            _metrics.RecordError(durationMetric, ex);
            throw;
        }
    }

    private void ExecuteWithMetrics(string durationMetric, string successMetric, string failureMetric, Action operation)
    {
        using var timer = _metrics.StartTimer(durationMetric);
        try {
            operation();
            _metrics.IncrementCounter(successMetric);
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(failureMetric);
            _metrics.RecordError(durationMetric, ex);
            throw;
        }
    }

    private PdfColumnarText BuildColumnarText(List<PdfWord> words, int columnCount, double? yTolerance)
    {
        if (columnCount < 1)
            throw new ArgumentOutOfRangeException(nameof(columnCount), "Column count must be at least 1.");

        if (words.Count == 0)
            return new(Enumerable.Repeat(string.Empty, columnCount).ToList());

        var tolerance = yTolerance ?? _options.DefaultYTolerance;
        if (columnCount == 1) {
            var singleLines = GroupIntoLines(words, tolerance).OrderByDescending(l => l.Y).ToList();
            return new([string.Join("\n", singleLines.Select(l => l.Text))]);
        }

        var linesForGutter = GroupIntoLines(words, tolerance).OrderByDescending(l => l.Y).ToList();
        var minX = words.Min(w => w.BoundingBox.Left);
        var maxX = words.Max(w => w.BoundingBox.Right);
        var width = maxX - minX;
        int[] columnOfWord;
        if (columnCount == 2 && width > 0) {
            var splitX = DetectTwoColumnSplitX(linesForGutter, minX, maxX);
            var boundary = splitX ?? minX + width * 0.5;
            columnOfWord = words.Select(w => CenterX(w) <= boundary ? 0 : 1).ToArray();
        }
        else if (width > 0)
            columnOfWord = words.Select(w => ColumnIndexEqualBands(CenterX(w), minX, maxX, columnCount)).ToArray();
        else
            columnOfWord = new int[words.Count];

        var columnTexts = new List<string>(columnCount);
        for (var c = 0; c < columnCount; c++) {
            var colWords = new List<PdfWord>();
            for (var i = 0; i < words.Count; i++) {
                if (columnOfWord[i] == c)
                    colWords.Add(words[i]);
            }

            if (colWords.Count == 0) {
                columnTexts.Add("");
                continue;
            }

            var colLines = GroupIntoLines(colWords, tolerance).OrderByDescending(l => l.Y).ToList();
            columnTexts.Add(string.Join("\n", colLines.Select(l => l.Text)));
        }

        return new(columnTexts);
    }

    private static double CenterX(PdfWord w) => (w.BoundingBox.Left + w.BoundingBox.Right) * 0.5;

    private static int ColumnIndexEqualBands(double centerX, double minX, double maxX, int columnCount)
    {
        if (maxX <= minX)
            return 0;

        var t = (centerX - minX) / (maxX - minX);
        var idx = (int)(t * columnCount);
        if (idx < 0)
            idx = 0;

        if (idx >= columnCount)
            idx = columnCount - 1;

        return idx;
    }

    /// <summary>Median X of the widest inter-word gap per line (when the gap clears a minimum), for two-column gutter detection.</summary>
    private static double? DetectTwoColumnSplitX(IReadOnlyList<PdfTextLine> lines, double minX, double maxX)
    {
        var width = maxX - minX;
        if (width <= 0)
            return null;

        var minGap = Math.Max(8.0, width * 0.025);
        var candidates = new List<double>();
        foreach (var line in lines) {
            var ws = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();
            if (ws.Count < 2)
                continue;

            double bestGap = 0;
            var bestMid = 0.0;
            for (var i = 0; i < ws.Count - 1; i++) {
                var gap = ws[i + 1].BoundingBox.Left - ws[i].BoundingBox.Right;
                if (gap > bestGap) {
                    bestGap = gap;
                    bestMid = (ws[i].BoundingBox.Right + ws[i + 1].BoundingBox.Left) * 0.5;
                }
            }

            if (bestGap >= minGap)
                candidates.Add(bestMid);
        }

        if (candidates.Count == 0)
            return null;

        candidates.Sort();
        return candidates[candidates.Count / 2];
    }

    /// <summary>Groups words into visual rows using proximity-based clustering. Words whose vertical mid-points are within yTolerance of each other are placed on the same line.</summary>
    private IReadOnlyList<PdfTextLine> GroupIntoLines(IEnumerable<PdfWord> words, double? yTolerance = null)
    {
        var tolerance = yTolerance ?? _options.DefaultYTolerance;
        var sorted = words.Select(w => (MidY: (w.BoundingBox.Top + w.BoundingBox.Bottom) * 0.5, Word: w)).OrderByDescending(x => x.MidY).ToList();
        if (sorted.Count == 0)
            return [];

        var groups = new List<(double CentroidY, List<PdfWord> Words)>();
        var centroidY = sorted[0].MidY;
        var current = new List<PdfWord> { sorted[0].Word };
        for (var i = 1; i < sorted.Count; i++) {
            var (midY, word) = sorted[i];
            if (Math.Abs(midY - centroidY) <= tolerance) {
                current.Add(word);
                centroidY = (centroidY * (current.Count - 1) + midY) / current.Count;
            }
            else {
                groups.Add((centroidY, current));
                centroidY = midY;
                current = [word];
            }
        }

        groups.Add((centroidY, current));
        return groups.Select(g => {
                var ordered = g.Words.OrderBy(w => w.BoundingBox.Left).ToList();
                return new PdfTextLine(g.CentroidY, ordered, string.Join(" ", ordered.Select(w => w.Text)));
            })
            .ToList();
    }

    /// <summary>Merges consecutive lines whose Y gap is ≤ threshold into single logical rows.</summary>
    private IReadOnlyList<PdfTextLine> MergeCloseLines(IReadOnlyList<PdfTextLine> lines, double threshold)
    {
        if (lines.Count == 0)
            return lines;

        var result = new List<PdfTextLine>();
        var pending = new List<PdfWord>(lines[0].Words);
        var pendingY = lines[0].Y;
        for (var i = 1; i < lines.Count; i++) {
            var gap = pendingY - lines[i].Y;
            if (gap <= threshold)
                pending.AddRange(lines[i].Words);
            else {
                result.Add(BuildLine(pendingY, pending));
                pending = new(lines[i].Words);
                pendingY = lines[i].Y;
            }
        }

        result.Add(BuildLine(pendingY, pending));
        return result;
    }

    private static PdfTextLine BuildLine(double y, List<PdfWord> words)
    {
        var ordered = words.OrderBy(w => w.BoundingBox.Left).ToList();
        return new(y, ordered, string.Join(" ", ordered.Select(w => w.Text)));
    }

    /// <summary>
    /// Extracts key/value pairs from words using known keys. Use <paramref name="keyValueColumnCount" /> &gt; 1 to split the region into that many vertical bands (e.g. duplicate keys side by side).
    /// </summary>
    private IReadOnlyList<KvColumnResult> ExtractKeyValueColumns(
        IReadOnlyList<PdfWord> words,
        IEnumerable<string> knownKeys,
        double? yTolerance = null,
        PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
        int keyValueColumnCount = 1)
    {
        if (words.Count == 0)
            return [new(0, new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase))];

        var tolerance = yTolerance ?? _options.DefaultYTolerance;
        var aliasToCanonical = BuildKnownKeyAliases(knownKeys);
        var keySet = new HashSet<string>(aliasToCanonical.Keys, StringComparer.OrdinalIgnoreCase);
        var explicitBands = ClampKvColumnBandCount(keyValueColumnCount);
        var columnCount = explicitBands > 1 ? explicitBands : 1;
        if (columnCount > 1) {
            var bandLists = BandWordsIntoVerticalColumns(words.ToList(), columnCount);
            var results = new List<KvColumnResult>();
            for (var col = 0; col < bandLists.Count; col++) {
                var dict = ExtractKeyValueFromWords(bandLists[col], aliasToCanonical, keySet, tolerance, keyValueLayout);
                results.Add(new(col, dict));
            }

            return results;
        }

        var singleResult = ExtractKeyValueFromWords(words.ToList(), aliasToCanonical, keySet, tolerance, keyValueLayout);
        return [new(0, singleResult)];
    }

    /// <summary>Splits words into <paramref name="columnCount" /> equal-width vertical bands by min/max X in the region.</summary>
    private static List<List<PdfWord>> BandWordsIntoVerticalColumns(IReadOnlyList<PdfWord> words, int columnCount)
    {
        if (words.Count == 0 || columnCount <= 1)
            return [words.ToList()];

        var minX = words.Min(w => w.BoundingBox.Left);
        var maxX = words.Max(w => w.BoundingBox.Right);
        var width = maxX - minX;
        if (width <= 0)
            return [words.ToList()];

        var results = new List<List<PdfWord>>(columnCount);
        for (var col = 0; col < columnCount; col++) {
            var colMinX = minX + col * width / columnCount;
            var colMaxX = minX + (col + 1) * width / columnCount;
            var colWords = words.Where(w => {
                    var x = w.BoundingBox.Left;
                    return x >= colMinX && x < colMaxX;
                })
                .ToList();
            results.Add(colWords);
        }

        return results;
    }

    /// <summary>Extracts key/value pairs from a single column of words.</summary>
    private Dictionary<string, string?> ExtractKeyValueFromWords(
        List<PdfWord> words,
        Dictionary<string, string> aliasToCanonical,
        HashSet<string> keySet,
        double tolerance,
        PdfKeyValueLayout keyValueLayout)
    {
        if (keyValueLayout == PdfKeyValueLayout.Vertical)
            return ExtractKeyValueFromWordsVertical(words, aliasToCanonical, keySet, tolerance);

        var continuationYGap = Math.Max(_options.MaxContinuationYGap, tolerance * 3);
        var rawLines = GroupIntoLines(words, tolerance);
        // Don't merge lines for key-value extraction - we want strict line boundaries
        var lines = rawLines.OrderByDescending(l => l.Y).ToList();
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        // Track active value columns: Key, ValueColumnLeft (where values start), LineY
        var activeValueColumns = new List<(string Key, double ValueColumnLeft, double Y)>();
        foreach (var line in lines) {
            var ws = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();
            if (ws.Count == 0)
                continue;

            // Check if this line contains any keys
            var keySpans = FindAllKeySpans(ws, keySet);
            if (keySpans.Count > 0) {
                // This line has keys - extract values from this line only
                // Clear active columns and set new ones based on this line
                activeValueColumns.Clear();
                var claimedIdxs = new HashSet<int>();
                foreach (var ks in keySpans) {
                    for (var i = ks.StartWordIdx; i <= ks.EndWordIdx; i++)
                        claimedIdxs.Add(i);
                }

                var keyRights = keySpans.Select(ks => ws[ks.EndWordIdx].BoundingBox.Right).ToList();
                var keyLefts = keySpans.Select(ks => ws[ks.StartWordIdx].BoundingBox.Left).ToList();
                for (var ki = 0; ki < keySpans.Count; ki++) {
                    var ks = keySpans[ki];
                    var canonicalKey = aliasToCanonical.TryGetValue(ks.Key, out var mappedKey) ? mappedKey : ks.Key;
                    var keyRight = keyRights[ki];
                    var nextKeyLeft = ki + 1 < keySpans.Count ? keyLefts[ki + 1] : double.MaxValue;

                    // Extract value words on THIS line only, between key's right edge and next key's left edge
                    var valueWords = ws.Select((w, idx) => (Word: w, Idx: idx))
                        .Where(x => !claimedIdxs.Contains(x.Idx) && x.Word.BoundingBox.Left >= keyRight + _options.DefaultKeyValueGap && x.Word.BoundingBox.Left < nextKeyLeft &&
                            !keySet.Contains(x.Word.Text.Trim()))
                        .Select(x => x.Word)
                        .ToList();

                    if (valueWords.Count > 0) {
                        var valueColumnLeft = valueWords[0].BoundingBox.Left;
                        var valueText = string.Join(" ", valueWords.Select(w => w.Text));

                        // Append to existing value if it exists (for multi-line values on same line as key)
                        result.TryGetValue(canonicalKey, out var existing);
                        result[canonicalKey] = string.IsNullOrWhiteSpace(existing) ? valueText : existing + " " + valueText;

                        // Track this value column for potential continuation
                        activeValueColumns.Add((canonicalKey, valueColumnLeft, line.Y));
                    }
                    else {
                        // No value on this line, but initialize the key and track for continuation
                        if (!result.ContainsKey(canonicalKey)) {
                            result[canonicalKey] = null;
                            // Still track the key position for continuation (use key's right edge)
                            activeValueColumns.Add((canonicalKey, keyRight + _options.DefaultKeyValueGap, line.Y));
                        }
                    }
                }
            }
            else {
                if (activeValueColumns.Count == 0)
                    continue;

                var lineText = line.Text.Trim();
                if (LooksLikeSectionHeader(lineText)) {
                    activeValueColumns.Clear();
                    continue;
                }

                // If a full key phrase appears, this is no longer a continuation line.
                if (FindAllKeySpans(ws, keySet).Count > 0) {
                    activeValueColumns.Clear();
                    continue;
                }

                for (var c = 0; c < activeValueColumns.Count; c++) {
                    var vc = activeValueColumns[c];
                    if (Math.Abs(line.Y - vc.Y) > continuationYGap)
                        continue;

                    var continuationWords = ws.Where(w
                            => w.BoundingBox.Left >= vc.ValueColumnLeft - _options.ValueColumnXTolerance &&
                            w.BoundingBox.Left <= vc.ValueColumnLeft + _options.MaxContinuationXDistance)
                        .ToList();

                    if (continuationWords.Count == 0)
                        continue;

                    var continuationText = string.Join(" ", continuationWords.Select(w => w.Text));
                    result.TryGetValue(vc.Key, out var existing);
                    result[vc.Key] = string.IsNullOrWhiteSpace(existing) ? continuationText : existing + " " + continuationText;

                    // Move the Y anchor down as we continue so multi-line values remain connected
                    activeValueColumns[c] = (vc.Key, vc.ValueColumnLeft, line.Y);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Primarily label-over-value (stacked) fields, but many PDFs mix that with label+value on one line (e.g. "Pages: 13 pages"). Same-line values are taken first; otherwise
    /// values are read from subsequent lines in the key's horizontal band.
    /// </summary>
    private Dictionary<string, string?> ExtractKeyValueFromWordsVertical(
        List<PdfWord> words,
        Dictionary<string, string> aliasToCanonical,
        HashSet<string> keySet,
        double tolerance)
    {
        var continuationYGap = Math.Max(_options.MaxContinuationYGap, tolerance * 3);
        var firstBlockMaxGap = Math.Max(_options.KeyValueStackedMaxFirstGap, Math.Max(continuationYGap * 2, _options.MaxContinuationYGap + tolerance * 4));
        var rawLines = GroupIntoLines(words, tolerance);
        var lines = rawLines.OrderByDescending(l => l.Y).ToList();
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var li = 0; li < lines.Count; li++) {
            var line = lines[li];
            var ws = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();
            if (ws.Count == 0)
                continue;

            var keySpans = FindAllKeySpans(ws, keySet);
            if (keySpans.Count == 0)
                continue;

            var keyLefts = keySpans.Select(ks => ws[ks.StartWordIdx].BoundingBox.Left).ToList();
            var keyY = line.Y;
            for (var ki = 0; ki < keySpans.Count; ki++) {
                var ks = keySpans[ki];
                var canonicalKey = aliasToCanonical.TryGetValue(ks.Key, out var mappedKey) ? mappedKey : ks.Key;
                var bandLeft = keyLefts[ki] - _options.ValueColumnXTolerance;
                var bandRight = ki + 1 < keySpans.Count ? keyLefts[ki + 1] - _options.DefaultKeyValueGap : double.MaxValue;

                var sameLineValueWords = GetSameLineValueWordsAfterKey(ws, keySpans, ki, keySet);
                if (sameLineValueWords.Count > 0) {
                    var sameLineText = string.Join(" ", sameLineValueWords.Select(w => w.Text)).Trim();
                    result.TryGetValue(canonicalKey, out var existingSame);
                    result[canonicalKey] = string.IsNullOrWhiteSpace(existingSame) ? sameLineText : existingSame + " " + sameLineText;
                    continue;
                }

                var pieces = new List<string>();
                double? lastContentY = null;
                double? valueColumnLeft = null;

                for (var lj = li + 1; lj < lines.Count; lj++) {
                    var vline = lines[lj];
                    var vwsOrdered = vline.Words.OrderBy(w => w.BoundingBox.Left).ToList();
                    if (vwsOrdered.Count == 0)
                        continue;

                    var gapFromKey = keyY - vline.Y;
                    if (lastContentY == null) {
                        if (gapFromKey > firstBlockMaxGap)
                            break;
                    }
                    else if (lastContentY.Value - vline.Y > continuationYGap) {
                        break;
                    }

                    if (LooksLikeSectionHeader(vline.Text.Trim()))
                        break;

                    if (FindAllKeySpans(vwsOrdered, keySet).Count > 0)
                        break;

                    List<PdfWord> bandWords;
                    if (valueColumnLeft == null) {
                        bandWords = vwsOrdered
                            .Where(w => w.BoundingBox.Left >= bandLeft && w.BoundingBox.Left < bandRight && !keySet.Contains(w.Text.Trim()))
                            .ToList();
                        if (bandWords.Count > 0)
                            valueColumnLeft = bandWords[0].BoundingBox.Left;
                    }
                    else {
                        bandWords = vwsOrdered.Where(w
                                => w.BoundingBox.Left >= valueColumnLeft.Value - _options.ValueColumnXTolerance &&
                                w.BoundingBox.Left <= valueColumnLeft.Value + _options.MaxContinuationXDistance &&
                                !keySet.Contains(w.Text.Trim()))
                            .ToList();
                    }

                    if (bandWords.Count == 0)
                        continue;

                    pieces.Add(string.Join(" ", bandWords.Select(w => w.Text)));
                    lastContentY = vline.Y;
                }

                var combined = string.Join(" ", pieces).Trim();
                if (string.IsNullOrWhiteSpace(combined)) {
                    if (!result.ContainsKey(canonicalKey))
                        result[canonicalKey] = null;
                }
                else {
                    result.TryGetValue(canonicalKey, out var existing);
                    result[canonicalKey] = string.IsNullOrWhiteSpace(existing) ? combined : existing + " " + combined;
                }
            }
        }

        return result;
    }

    /// <summary>Words on the same line as the key, to the right of the key span (horizontal strip), excluding other keys.</summary>
    private List<PdfWord> GetSameLineValueWordsAfterKey(List<PdfWord> ws, IReadOnlyList<KeySpan> keySpans, int ki, HashSet<string> keySet)
    {
        var claimedIdxs = new HashSet<int>();
        foreach (var ksp in keySpans) {
            for (var i = ksp.StartWordIdx; i <= ksp.EndWordIdx; i++)
                claimedIdxs.Add(i);
        }

        var keyRights = keySpans.Select(ks => ws[ks.EndWordIdx].BoundingBox.Right).ToList();
        var keyLefts = keySpans.Select(ks => ws[ks.StartWordIdx].BoundingBox.Left).ToList();
        var keyRight = keyRights[ki];
        var nextKeyLeft = ki + 1 < keySpans.Count ? keyLefts[ki + 1] : double.MaxValue;
        return ws.Select((w, idx) => (Word: w, Idx: idx))
            .Where(x => !claimedIdxs.Contains(x.Idx) && x.Word.BoundingBox.Left >= keyRight + _options.DefaultKeyValueGap && x.Word.BoundingBox.Left < nextKeyLeft &&
                !keySet.Contains(x.Word.Text.Trim()))
            .Select(x => x.Word)
            .ToList();
    }

    private List<KeySpan> FindAllKeySpans(List<PdfWord> words, HashSet<string> keySet)
    {
        var spans = new List<KeySpan>();
        var keyPrefixes = BuildPrefixSet(keySet);
        for (var i = 0; i < words.Count; i++) {
            var candidate = new StringBuilder();
            var bestEnd = -1;
            string? bestKey = null;
            for (var j = i; j < words.Count; j++) {
                if (candidate.Length > 0)
                    candidate.Append(' ');

                candidate.Append(words[j].Text);
                var candidateStr = candidate.ToString();
                if (!keyPrefixes.Contains(candidateStr))
                    break;

                if (!keySet.Contains(candidateStr))
                    continue;

                bestEnd = j;
                bestKey = candidateStr;
            }

            if (bestKey == null)
                continue;

            spans.Add(new(bestKey, i, bestEnd));
            i = bestEnd;
        }

        return spans;
    }

    private HashSet<string> BuildPrefixSet(HashSet<string> keySet)
    {
        var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keySet) {
            var tokens = key.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var token in tokens) {
                if (sb.Length > 0)
                    sb.Append(' ');

                sb.Append(token);
                prefixes.Add(sb.ToString());
            }
        }

        return prefixes;
    }

    private static bool LooksLikeSectionHeader(string lineText)
    {
        if (string.IsNullOrWhiteSpace(lineText))
            return false;

        var words = lineText.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        return (lineText.Length > 5 && words.Length <= 5 && lineText.All(c => char.IsUpper(c) || char.IsWhiteSpace(c) || char.IsDigit(c) || c == '/' || c == '-' || c == ':')) ||
            (words.Length == 1 && lineText.Length > 3 && lineText.All(char.IsUpper));
    }

    /// <summary>Extracts a table from words using column headers. Data rows are expected to be below the header row.</summary>
    private (IReadOnlyList<IDataTableCell> HeaderCells, IReadOnlyList<IReadOnlyDictionary<string, IDataTableCell>> Rows) ExtractTableFromWordsFormatted(
        IReadOnlyList<PdfWord> words,
        ColumnHeader[] headers,
        double? yTolerance = null,
        PdfInferFormattingFlags? inferFormattingForHeaderRows = null)
    {
        if (words.Count == 0 || headers.Length == 0)
            return ([], []);

        ArgumentHelpers.ThrowIfNullOrEmpty(headers, nameof(headers));
        var tolerance = yTolerance ?? _options.DefaultYTolerance;
        var allLines = GroupIntoLinesPreservingInputOrder(words, tolerance);
        var headerLabels = headers.Select(h => h.Label).ToArray();

        // Find the first matching header row in reading order.
        PdfTextLine? headerLine = null;
        var headerLineIndex = -1;
        var headerLinesSpanned = 1;
        List<PdfWord> headerWords = new();
        for (var li = 0; li < allLines.Count; li++) {
            var spanWords = new List<PdfWord>(allLines[li].Words);
            var mergedLines = 1;
            var j = li;
            while (j + 1 < allLines.Count
                && allLines[j].Y - allLines[j + 1].Y <= _options.TableHeaderMergeThreshold
                && inferFormattingForHeaderRows is PdfInferFormattingFlags hf
                && (hf & (PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline)) != 0
                && LineQualifiesForHeaderBlockExtension(allLines[j], allLines[j + 1], hf)) {
                spanWords.AddRange(allLines[j + 1].Words);
                mergedLines++;
                j++;
            }

            var mergedForHits = string.Join(" ", Enumerable.Range(0, mergedLines).Select(k => allLines[li + k].Text));
            var candidateHits = headerLabels.Count(h => LineContainsHeaderLabel(mergedForHits, h));
            var minRequired = Math.Ceiling(headerLabels.Length * _options.TableHeaderMatchThreshold);
            if (!(candidateHits >= minRequired))
                continue;

            headerWords = spanWords.OrderBy(w => w.BoundingBox.Left).ToList();
            headerLine = allLines[li];
            headerLineIndex = li;
            headerLinesSpanned = mergedLines;
            break;
        }

        if (headerLine == null || headerWords.Count == 0 || headerLineIndex < 0)
            return ([], []);

        // Map headers to column X positions
        var columnPositions = new List<(ColumnHeader Header, double StartX)>();
        foreach (var header in headers) {
            if (TryFindHeaderStartX(headerWords, header.Label, out var startX))
                columnPositions.Add((header, startX));
        }

        if (columnPositions.Count == 0)
            return ([], []);

        columnPositions = columnPositions.OrderBy(c => c.StartX).ToList();

        // Build formatted header cells from actual PDF header words
        var headerCells = new List<IDataTableCell>();
        for (var colIndex = 0; colIndex < columnPositions.Count; colIndex++) {
            var col = columnPositions[colIndex];
            var nextStart = colIndex + 1 < columnPositions.Count ? columnPositions[colIndex + 1].StartX : double.MaxValue;
            var startX = Math.Max(double.MinValue, col.StartX - _options.TableColumnXTolerance);
            var endX = nextStart - _options.TableColumnXTolerance;
            var cellWords = headerWords.Where(w => w.BoundingBox.Left >= startX && w.BoundingBox.Left < endX).ToList();
            headerCells.Add(BuildCellFromWords(cellWords));
        }

        // When the header spans two visual lines, both were merged into headerWords — data must start after the second line.
        var dataLines = allLines.Skip(headerLineIndex + headerLinesSpanned).ToList();
        var rows = new List<Dictionary<string, IDataTableCell>>();
        foreach (var line in dataLines) {
            if (string.IsNullOrWhiteSpace(line.Text))
                continue;

            var row = new Dictionary<string, IDataTableCell>(StringComparer.OrdinalIgnoreCase);
            var orderedWords = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();

            // Left-aligned table assumption:
            // a column owns words between its start X and the next column's start X.
            for (var colIndex = 0; colIndex < columnPositions.Count; colIndex++) {
                var col = columnPositions[colIndex];
                var nextStart = colIndex + 1 < columnPositions.Count ? columnPositions[colIndex + 1].StartX : double.MaxValue;
                var startX = Math.Max(double.MinValue, col.StartX - _options.TableColumnXTolerance);
                var endX = nextStart - _options.TableColumnXTolerance;
                var cellWords = orderedWords.Where(w => w.BoundingBox.Left >= startX && w.BoundingBox.Left < endX).ToList();
                row[col.Header.Label] = BuildCellFromWords(cellWords);
            }

            if (IsHeaderEchoRowFormatted(row, headers))
                continue;

            if (row.Values.Any(v => !string.IsNullOrWhiteSpace(v.DisplayValue)))
                rows.Add(row);
        }

        // Merge multi-line rows using explicit key columns.
        var keyLabels = new HashSet<string>(headers.Where(h => h.IsKey).Select(h => h.Label), StringComparer.OrdinalIgnoreCase);
        if (keyLabels.Count == 0)
            return (headerCells, rows);

        var mergedRows = new List<IReadOnlyDictionary<string, IDataTableCell>>();
        Dictionary<string, IDataTableCell>? currentRow = null;
        foreach (var row in rows) {
            var hasKey = keyLabels.Any(label => {
                row.TryGetValue(label, out var cell);
                return !string.IsNullOrWhiteSpace(cell?.DisplayValue);
            });

            if (hasKey) {
                if (currentRow != null)
                    mergedRows.Add(currentRow);

                currentRow = new(row, StringComparer.OrdinalIgnoreCase);
            }
            else if (currentRow != null) {
                foreach (var kvp in row) {
                    if (string.IsNullOrWhiteSpace(kvp.Value.DisplayValue))
                        continue;

                    currentRow.TryGetValue(kvp.Key, out var existing);
                    currentRow[kvp.Key] = CombineCells(existing, kvp.Value);
                }
            }
            else
                currentRow = new(row, StringComparer.OrdinalIgnoreCase);
        }

        if (currentRow != null)
            mergedRows.Add(currentRow);

        return (headerCells, mergedRows);
    }

    private static IDataTableCell BuildCellFromWords(List<PdfWord> cellWords)
    {
        if (cellWords.Count == 0)
            return DataTableCell.FromValue("");

        var text = string.Join(" ", cellWords.Select(w => w.Text));
        var firstWithFormat = cellWords.FirstOrDefault(w => w.Format != null)?.Format;
        if (firstWithFormat == null)
            return DataTableCell.FromValue(text);

        return new DataTableCell<string>(
            text,
            firstWithFormat.FontSize,
            firstWithFormat.FontName,
            firstWithFormat.FontBold,
            firstWithFormat.FontItalic,
            firstWithFormat.FontUnderline,
            null,
            firstWithFormat.FontColor);
    }

    private static IDataTableCell CombineCells(IDataTableCell? a, IDataTableCell b)
    {
        if (a == null || string.IsNullOrWhiteSpace(a.DisplayValue))
            return b;

        if (string.IsNullOrWhiteSpace(b.DisplayValue))
            return a;

        var value = a.DisplayValue + " " + b.DisplayValue;
        return new DataTableCell<string>(value, a.FontSize, a.FontName, a.FontBold, a.FontItalic, a.FontUnderline, a.FontStrikethrough, a.FontColor);
    }

    private static bool IsHeaderEchoRowFormatted(Dictionary<string, IDataTableCell> row, ColumnHeader[] headers)
    {
        var strRow = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in row)
            strRow[kv.Key] = kv.Value.DisplayValue;

        return IsHeaderEchoRow(strRow, headers);
    }

    private IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTableFromWords(
        IReadOnlyList<PdfWord> words,
        ColumnHeader[] headers,
        double? yTolerance = null,
        PdfInferFormattingFlags? inferFormattingForHeaderRows = null)
    {
        var (_, rows) = ExtractTableFromWordsFormatted(words, headers, yTolerance, inferFormattingForHeaderRows);
        return rows.Select(r => (IReadOnlyDictionary<string, string?>)r.ToDictionary(kv => kv.Key, kv => kv.Value.DisplayValue, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    private static List<PdfTextLine> GroupIntoLinesPreservingInputOrder(IReadOnlyList<PdfWord> words, double yTolerance)
    {
        if (words.Count == 0)
            return [];

        var ordered = words.ToList();
        var lines = new List<PdfTextLine>();
        var currentWords = new List<PdfWord> { ordered[0] };
        var currentY = (ordered[0].BoundingBox.Top + ordered[0].BoundingBox.Bottom) * 0.5;
        var lastX = ordered[0].BoundingBox.Left;
        for (var i = 1; i < ordered.Count; i++) {
            var word = ordered[i];
            var midY = (word.BoundingBox.Top + word.BoundingBox.Bottom) * 0.5;
            var x = word.BoundingBox.Left;
            var sameVisualLine = Math.Abs(midY - currentY) <= yTolerance;
            var continuesLeftToRight = x >= lastX - yTolerance;
            if (sameVisualLine && continuesLeftToRight) {
                currentWords.Add(word);
                currentY = (currentY * (currentWords.Count - 1) + midY) / currentWords.Count;
                lastX = x;
                continue;
            }

            var finalized = currentWords.OrderBy(w => w.BoundingBox.Left).ToList();
            lines.Add(new(currentY, finalized, string.Join(" ", finalized.Select(w => w.Text))));
            currentWords = [word];
            currentY = midY;
            lastX = x;
        }

        var lastLineWords = currentWords.OrderBy(w => w.BoundingBox.Left).ToList();
        lines.Add(new(currentY, lastLineWords, string.Join(" ", lastLineWords.Select(w => w.Text))));
        return lines;
    }

    private static bool TryFindHeaderStartX(IReadOnlyList<PdfWord> headerWords, string headerLabel, out double startX)
    {
        startX = 0;
        var tokens = SplitHeaderTokens(headerLabel);
        if (tokens.Count == 0)
            return false;

        var ordered = headerWords.OrderBy(w => w.BoundingBox.Left).ToList();
        var normalizedWords = ordered.Select(w => NormalizeToken(w.Text)).ToList();
        for (var i = 0; i < normalizedWords.Count; i++) {
            if (!string.Equals(normalizedWords[i], tokens[0], StringComparison.OrdinalIgnoreCase))
                continue;

            var match = true;
            for (var j = 1; j < tokens.Count; j++) {
                var idx = i + j;
                if (idx >= normalizedWords.Count || !string.Equals(normalizedWords[idx], tokens[j], StringComparison.OrdinalIgnoreCase)) {
                    match = false;
                    break;
                }
            }

            if (!match)
                continue;

            startX = ordered[i].BoundingBox.Left;
            return true;
        }

        // Fallback to the first token match if full phrase isn't contiguous.
        var fallbackIdx = normalizedWords.FindIndex(w => string.Equals(w, tokens[0], StringComparison.OrdinalIgnoreCase));
        if (fallbackIdx < 0)
            return false;

        startX = ordered[fallbackIdx].BoundingBox.Left;
        return true;
    }

    private static List<string> SplitHeaderTokens(string text)
        => text.Split([' '], StringSplitOptions.RemoveEmptyEntries).Select(NormalizeToken).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

    private static bool LineContainsHeaderLabel(string lineText, string headerLabel)
    {
        var lineTokens = SplitHeaderTokens(lineText);
        var labelTokens = SplitHeaderTokens(headerLabel);
        if (lineTokens.Count == 0 || labelTokens.Count == 0 || lineTokens.Count < labelTokens.Count)
            return false;

        for (var i = 0; i <= lineTokens.Count - labelTokens.Count; i++) {
            var allMatch = true;
            for (var j = 0; j < labelTokens.Count; j++) {
                if (string.Equals(lineTokens[i + j], labelTokens[j], StringComparison.OrdinalIgnoreCase))
                    continue;

                allMatch = false;
                break;
            }

            if (allMatch)
                return true;
        }

        return false;
    }

    private static bool IsHeaderEchoRow(IReadOnlyDictionary<string, string?> row, ColumnHeader[] headers)
    {
        foreach (var header in headers) {
            row.TryGetValue(header.Label, out var value);
            if (!string.Equals(NormalizeToken(value ?? string.Empty), NormalizeToken(header.Label), StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static Dictionary<string, string> BuildKnownKeyAliases(IEnumerable<string> knownKeys)
    {
        var keys = knownKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var aliasToCanonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys) {
            aliasToCanonical[key] = key;
            // PDFs often attach ':' to the label word ("Pages:") while users type "Pages".
            if (!key.EndsWith(":") && !aliasToCanonical.ContainsKey(key + ":"))
                aliasToCanonical[key + ":"] = key;
        }

        return aliasToCanonical;
    }

    private static string NormalizeKeyAlias(string text) => new string(text.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static string NormalizeToken(string token) => new(token.Where(c => char.IsLetterOrDigit(c) || c == '#').ToArray());

    /// <summary>Merges a trailing "Dt." / "Dt" token into the previous header when PDF word segmentation splits "Offense Dt." into two columns.</summary>
    private static List<string> MergeOrphanDtHeaderTokens(IReadOnlyList<string> labels)
    {
        if (labels.Count < 2)
            return labels.ToList();

        var list = new List<string>();
        for (var i = 0; i < labels.Count; i++) {
            if (i < labels.Count - 1
                && string.Equals(NormalizeToken(labels[i + 1]), "Dt", StringComparison.OrdinalIgnoreCase)
                && labels[i + 1].Length <= 12) {
                list.Add((labels[i].Trim() + " " + labels[i + 1].Trim()).Trim());
                i++;
                continue;
            }

            list.Add(labels[i]);
        }

        return list;
    }

    private readonly record struct KeySpan(string Key, int StartWordIdx, int EndWordIdx);
}