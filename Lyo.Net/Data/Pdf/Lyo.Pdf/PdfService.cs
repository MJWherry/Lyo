using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Pdf.Editing;
using Lyo.Pdf.Internal;
using Lyo.Pdf.Models;
using Microsoft.Extensions.Logging;
using PdfSharp.Pdf.IO;
using PdfReader = Lyo.Pdf.Documents.PdfReader;
using PigDoc = UglyToad.PdfPig.PdfDocument;
using SharpPdfDoc = PdfSharp.Pdf.PdfDocument;
using SharpPdfReaderIO = PdfSharp.Pdf.IO.PdfReader;

namespace Lyo.Pdf;

public sealed class PdfService : IPdfService
{
    private readonly HttpClient? _httpClient;
    private readonly ILogger<PdfService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly long _maxPdfSizeBytes;
    private readonly IMetrics _metrics;
    private readonly PdfServiceOptions _options;

    public PdfService(ILoggerFactory loggerFactory, IMetrics? metrics = null, HttpClient? httpClient = null, PdfServiceOptions? options = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<PdfService>();
        options ??= new();
        _options = options;
        _metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _httpClient = httpClient;
        _maxPdfSizeBytes = options.MaxPdfSizeBytes.GetValueOrDefault() > 0 ? options.MaxPdfSizeBytes!.Value : PdfServiceOptions.SuggestedMaxPdfSizeBytes;
    }

    /// <inheritdoc />
    public async Task<IPdfReader> OpenFromFileAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(filePath);
#if NETSTANDARD2_0
        var bytes = await Task.Run(() => File.ReadAllBytes(filePath), ct).ConfigureAwait(false);
#else
        var bytes = await File.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);
#endif
        return OpenFromBuffer(bytes, filePath);
    }

    /// <inheritdoc />
    public IPdfReader OpenFromFile(string filePath)
    {
        ArgumentHelpers.ThrowIfFileNotFound(filePath);
        return OpenFromBuffer(File.ReadAllBytes(filePath), filePath);
    }

    /// <inheritdoc />
    public async Task<IPdfReader> OpenFromUrlAsync(string url, CancellationToken ct = default)
    {
        UriHelpers.ThrowIfInvalidUri(url);
        var client = _httpClient ?? new HttpClient();
        try {
#if NETSTANDARD2_0
            var bytes = await client.GetByteArrayAsync(url).ConfigureAwait(false);
#else
            var bytes = await client.GetByteArrayAsync(url, ct).ConfigureAwait(false);
#endif
            return OpenFromBuffer(bytes, url: url);
        }
        finally {
            if (_httpClient == null)
                client.Dispose();
        }
    }

    /// <inheritdoc />
    public Task<IPdfReader> OpenFromBytesAsync(byte[] pdfBytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(pdfBytes);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(OpenFromBytes(pdfBytes));
    }

    /// <inheritdoc />
    public IPdfReader OpenFromBytes(byte[] pdfBytes)
    {
        ArgumentHelpers.ThrowIfNull(pdfBytes);
        return OpenFromBuffer(pdfBytes);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IPdfReader>> OpenFromFilesAsync(IEnumerable<string> filePaths, CancellationToken ct = default)
    {
        var paths = filePaths.AsReadOnlyList();
        ArgumentHelpers.ThrowIfNullOrEmpty(paths);
        foreach (var path in paths)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);

        var list = new List<IPdfReader>(paths.Count);
        try {
            foreach (var path in paths) {
                ct.ThrowIfCancellationRequested();
                list.Add(await OpenFromFileAsync(path, ct).ConfigureAwait(false));
            }

            return list;
        }
        catch {
            DisposeReaderList(list);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IPdfReader> OpenFromFiles(params string[] filePaths) => OpenBatchSync(filePaths, OpenFromFile);

    /// <inheritdoc />
    public async Task<IReadOnlyList<IPdfReader>> OpenFromUrlsAsync(IEnumerable<string> urls, CancellationToken ct = default)
    {
        var urlList = urls.AsReadOnlyList();
        ArgumentHelpers.ThrowIfNullOrEmpty(urlList);
        foreach (var url in urlList)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(url);

        var list = new List<IPdfReader>(urlList.Count);
        try {
            foreach (var url in urlList) {
                ct.ThrowIfCancellationRequested();
                list.Add(await OpenFromUrlAsync(url, ct).ConfigureAwait(false));
            }

            return list;
        }
        catch {
            DisposeReaderList(list);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IPdfReader>> OpenFromBytesBatchAsync(IEnumerable<byte[]> pdfs, CancellationToken ct = default)
    {
        var list = pdfs.AsReadOnlyList();
        OperationHelpers.ThrowIf(list.Count == 0, "At least one PDF buffer is required.");
        foreach (var pdf in list)
            ArgumentHelpers.ThrowIfNullOrEmpty(pdf);

        ct.ThrowIfCancellationRequested();
        return Task.FromResult(OpenFromBytesBatch(list.ToArray()));
    }

    /// <inheritdoc />
    public IReadOnlyList<IPdfReader> OpenFromBytesBatch(params byte[][] pdfs)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(pdfs);
        foreach (var pdf in pdfs)
            ArgumentHelpers.ThrowIfNullOrEmpty(pdf);

        return OpenBatchSync(pdfs, OpenFromBytes);
    }

    /// <inheritdoc />
    public Task<IPdfReader> OpenFromStreamAsync(Stream stream, CancellationToken ct = default) => Task.Run(() => OpenFromStream(stream), ct);

    /// <inheritdoc />
    public IPdfReader OpenFromStream(Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        using var memoryStream = new MemoryStream();
        if (stream.CanSeek)
            stream.Position = 0;

        stream.CopyTo(memoryStream);
        return OpenFromBuffer(memoryStream.ToArray());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IPdfReader>> OpenFromStreamsAsync(IEnumerable<Stream> streams, CancellationToken ct = default)
    {
        var streamList = streams.AsReadOnlyList();
        ArgumentHelpers.ThrowIfNullOrEmpty(streamList);
        var list = new List<IPdfReader>(streamList.Count);
        try {
            foreach (var stream in streamList)
                list.Add(await OpenFromStreamAsync(stream, ct).ConfigureAwait(false));

            return list;
        }
        catch {
            DisposeReaderList(list);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IPdfReader> OpenFromStreams(params Stream[] streams) => OpenBatchSync(streams, OpenFromStream);

    /// <inheritdoc />
    public IPdfWriter CreateEmpty()
    {
        var doc = new SharpPdfDoc();
        doc.AddPage();
        return new PdfWriter(doc);
    }

    /// <inheritdoc />
    public IPdfWriter OpenForEdit(byte[] pdfBytes)
    {
        ArgumentHelpers.ThrowIfNull(pdfBytes);
        ArgumentHelpers.ThrowIfNullOrEmpty(pdfBytes);
        var copy = (byte[])pdfBytes.Clone();
        return new PdfWriter(OpenForEditSharpInternal(copy));
    }

    /// <inheritdoc />
    public IPdfWriter OpenForEdit(string filePath)
    {
        ArgumentHelpers.ThrowIfFileNotFound(filePath);
        return OpenForEdit(File.ReadAllBytes(filePath));
    }

    /// <inheritdoc />
    public IPdfWriter OpenForEdit(Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        using var memoryStream = new MemoryStream();
        if (stream.CanSeek)
            stream.Position = 0;

        stream.CopyTo(memoryStream);
        return OpenForEdit(memoryStream.ToArray());
    }

    /// <inheritdoc />
    public Task<IPdfWriter> OpenForEditAsync(byte[] pdfBytes, CancellationToken ct = default) => Task.Run(() => OpenForEdit(pdfBytes), ct);

    /// <inheritdoc />
    public Task<IPdfWriter> OpenForEditAsync(string filePath, CancellationToken ct = default) => Task.Run(() => OpenForEdit(filePath), ct);

    /// <inheritdoc />
    public Task<IPdfWriter> OpenForEditAsync(Stream stream, CancellationToken ct = default) => Task.Run(() => OpenForEdit(stream), ct);

    /// <inheritdoc />
    public Task<byte[]> MergePdfsToFileAsync(IReadOnlyList<byte[]> pdfBuffers, string filePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath);
        return Task.Run(
            async () => {
                var bytes = await MergePdfsAsync(pdfBuffers, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

#if NETSTANDARD2_0
                File.WriteAllBytes(filePath, bytes);
#else
                await File.WriteAllBytesAsync(filePath, bytes, ct).ConfigureAwait(false);
#endif
                return bytes;
            }, ct);
    }

    /// <inheritdoc />
    public byte[] MergePdfsToFile(IReadOnlyList<byte[]> pdfBuffers, string filePath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath);
        var bytes = MergePdfs(pdfBuffers);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllBytes(filePath, bytes);
        return bytes;
    }

    /// <inheritdoc />
    public async Task<byte[]> MergePdfsToStreamAsync(IReadOnlyList<byte[]> pdfBuffers, Stream stream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotWritable(stream, $"Stream '{nameof(stream)}' must be writable.");
        var bytes = await MergePdfsAsync(pdfBuffers, ct).ConfigureAwait(false);
        if (stream.CanSeek)
            stream.SetLength(0);

        await stream.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
        return bytes;
    }

    /// <inheritdoc />
    public byte[] MergePdfsToStream(IReadOnlyList<byte[]> pdfBuffers, Stream stream)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotWritable(stream, $"Stream '{nameof(stream)}' must be writable.");
        var bytes = MergePdfs(pdfBuffers);
        if (stream.CanSeek)
            stream.SetLength(0);

        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
        return bytes;
    }

    /// <inheritdoc />
    public Task<byte[]> MergePdfsAsync(IReadOnlyList<byte[]> pdfBuffers, CancellationToken ct = default) => Task.Run(() => MergePdfs(pdfBuffers), ct);

    /// <inheritdoc />
    public byte[] MergePdfs(IReadOnlyList<byte[]> pdfBuffers) => MergePdfsInternal(pdfBuffers);

    /// <inheritdoc />
    public async Task<byte[]> MergePdfFilesAsync(string outputFilePath, string initialPdf, string[] paths, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath);
        var buffers = CollectFileBuffers(initialPdf, paths);
        return await MergePdfsToFileAsync(buffers, outputFilePath, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public byte[] MergePdfFiles(string outputFilePath, string initialPdf, params string[] paths)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath);
        var buffers = CollectFileBuffers(initialPdf, paths);
        return MergePdfsToFile(buffers, outputFilePath);
    }

    /// <inheritdoc />
    public Task<byte[]> MergePdfFilesAsync(string outputFilePath, FileInfo initialPdf, FileInfo[] paths, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(initialPdf);
        ArgumentHelpers.ThrowIfNull(paths);
        var fullPaths = paths.Select(p => p.FullName).ToArray();
        return MergePdfFilesAsync(outputFilePath, initialPdf.FullName, fullPaths, ct);
    }

    /// <inheritdoc />
    public byte[] MergePdfFiles(string outputFilePath, FileInfo initialPdf, params FileInfo[] paths)
    {
        ArgumentHelpers.ThrowIfNull(initialPdf);
        ArgumentHelpers.ThrowIfNull(paths);
        var fullPaths = paths.Select(p => p.FullName).ToArray();
        return MergePdfFiles(outputFilePath, initialPdf.FullName, fullPaths);
    }

    /// <inheritdoc />
    public Task<byte[]> MergePdfBytesAsync(string outputFilePath, byte[] initialPdf, byte[][] pdfs, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath);
        var buffers = CollectByteBuffers(initialPdf, pdfs);
        return MergePdfsToFileAsync(buffers, outputFilePath, ct);
    }

    /// <inheritdoc />
    public byte[] MergePdfBytes(string outputFilePath, byte[] initialPdf, params byte[][] pdfs)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath);
        var buffers = CollectByteBuffers(initialPdf, pdfs);
        return MergePdfsToFile(buffers, outputFilePath);
    }

    private IReadOnlyList<IPdfReader> OpenBatchSync<T>(IReadOnlyList<T> inputs, Func<T, IPdfReader> opener)
    {
        var list = new List<IPdfReader>(inputs.Count);
        try {
            foreach (var input in inputs)
                list.Add(opener(input));

            return list;
        }
        catch {
            DisposeReaderList(list);
            throw;
        }
    }

    private void DisposeReaderList(List<IPdfReader> list)
    {
        foreach (var doc in list) {
            try {
                doc.Dispose();
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to dispose partially opened {ReaderType}.", nameof(PdfReader));
            }
        }
    }

    private PdfReader OpenFromBuffer(byte[] pdfBytes, string? filePath = null, string? url = null)
        => PdfOperationMetrics.Execute(
            _metrics, Constants.Metrics.LoadDuration, Constants.Metrics.LoadSuccess, Constants.Metrics.LoadFailure, () => {
                ArgumentHelpers.ThrowIfGreaterThan(
                    pdfBytes.Length, _maxPdfSizeBytes, nameof(pdfBytes), $"PDF size ({pdfBytes.Length} bytes) exceeds max allowed size ({_maxPdfSizeBytes} bytes).");

                var pig = PigDoc.Open(pdfBytes);
                _logger.LogDebug("Opened {ReaderType} with {PageCount} page(s); source={Source}", nameof(PdfReader), pig.NumberOfPages, filePath ?? url ?? "bytes");
                return new PdfReader(_loggerFactory, _options, _metrics, pdfBytes, pig, filePath, url);
            });

    private SharpPdfDoc OpenForEditSharpInternal(byte[] copy)
        => PdfOperationMetrics.Execute(
            _metrics, Constants.Metrics.MergeDuration, Constants.Metrics.MergeSuccess, Constants.Metrics.MergeFailure,
            () => SharpPdfReaderIO.Open(new MemoryStream(copy, false), PdfDocumentOpenMode.Modify));

    private byte[] MergePdfsInternal(IReadOnlyList<byte[]> pdfBuffers)
        => PdfOperationMetrics.Execute(
            _metrics, Constants.Metrics.MergeDuration, Constants.Metrics.MergeSuccess, Constants.Metrics.MergeFailure, () => {
                ArgumentHelpers.ThrowIfNull(pdfBuffers);
                ArgumentHelpers.ThrowIf(pdfBuffers.Count < 2, "At least two PDFs are required to merge.", nameof(pdfBuffers));
                using var outputDocument = new SharpPdfDoc();
                foreach (var buffer in pdfBuffers) {
                    ArgumentHelpers.ThrowIfNullOrEmpty(buffer);
                    using var inputStream = new MemoryStream(buffer, false);
                    using var sourceDocument = SharpPdfReaderIO.Open(inputStream, PdfDocumentOpenMode.Import);
                    for (var pageIndex = 0; pageIndex < sourceDocument.PageCount; pageIndex++)
                        outputDocument.AddPage(sourceDocument.Pages[pageIndex]);
                }

                using var outputStream = new MemoryStream();
                outputDocument.Save(outputStream);
                return outputStream.ToArray();
            });

    private static List<byte[]> CollectFileBuffers(string initialPdf, IEnumerable<string> paths)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(initialPdf);
        ArgumentHelpers.ThrowIfNull(paths);
        var normalized = new List<string> { initialPdf };
        foreach (var path in paths) {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(paths));
            normalized.Add(path);
        }

        ArgumentHelpers.ThrowIf(normalized.Count < 2, "At least two PDFs are required to merge.", nameof(paths));
        return normalized.Select(File.ReadAllBytes).ToList();
    }

    private static List<byte[]> CollectByteBuffers(byte[] initialPdf, IEnumerable<byte[]> pdfs)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(initialPdf);
        ArgumentHelpers.ThrowIfNull(pdfs);
        var normalized = new List<byte[]> { initialPdf };
        foreach (var pdf in pdfs) {
            ArgumentHelpers.ThrowIfNullOrEmpty(pdf, nameof(pdfs));
            normalized.Add(pdf);
        }

        ArgumentHelpers.ThrowIf(normalized.Count < 2, "At least two PDFs are required to merge.", nameof(pdfs));
        return normalized;
    }
}