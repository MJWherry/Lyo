using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using EasyCompressor;
using Lyo.Common.Records;
using Lyo.Compression.Models;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Streams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Compression;

/// <summary>
/// Default <see cref="ICompressionService" /> implementation backed by registered <see cref="ICompressorFactory" /> instances, with optional
/// <see cref="Lyo.Metrics.IMetrics" /> emission.
/// </summary>
/// <remarks>
/// <para>
/// The compressor and algorithm are fixed after construction. Safe to resolve as a singleton and call concurrently; batch APIs use parallel workers bounded by
/// <see cref="CompressionServiceOptions.MaxParallelFileOperations" />.
/// </para>
/// <para>
/// Algorithm resolution is dispatched through the injected <see cref="ICompressorFactory" /> collection: the base package registers GZip/Deflate (and Brotli/ZLib on net10+);
/// addon packages such as <c>Lyo.Compression.LZ4</c> register their own factories via their <c>services.Add{Algo}Compressor()</c> extension.
/// </para>
/// </remarks>
public sealed class CompressionService : ICompressionService, ICompressionResolver
{
    private readonly ILogger<CompressionService> _logger;

    private readonly CompressionServiceOptions _options;

    private readonly ICompressor _compressor;

    private readonly ConcurrentDictionary<(CompressionAlgorithm Algorithm, CompressionLevel Level), ICompressor> _compressorCache = new();

    private readonly IMetrics _metrics;

    private readonly IReadOnlyDictionary<CompressionAlgorithm, ICompressorFactory> _factories;

    private readonly Func<ICompressionResolver>? _resolveResolver;

    /// <inheritdoc />
    public CompressionAlgorithm Algorithm { get; }

    /// <inheritdoc />
    public string FileExtension => Algorithm.Extension;

    /// <inheritdoc />
    public ICompressionResolver Resolver => _resolveResolver?.Invoke() ?? this;

    /// <inheritdoc />
    public ICompressionAlgorithmSelector? AlgorithmSelector { get; }

    public CompressionService(
        IEnumerable<ICompressorFactory>? factories = null,
        ILogger<CompressionService>? logger = null,
        CompressionServiceOptions? options = null,
        IMetrics? metrics = null,
        Func<ICompressionResolver>? resolveResolver = null,
        ICompressionAlgorithmSelector? algorithmSelector = null)
    {
        _logger = logger ?? NullLoggerFactory.Instance.CreateLogger<CompressionService>();
        _options = options ?? CompressionServiceOptions.Default;
        ArgumentHelpers.ThrowIfNullOrNotInRange(
            _options.MaxParallelFileOperations, 1, int.MaxValue, nameof(options) + "." + nameof(CompressionServiceOptions.MaxParallelFileOperations));

        ArgumentHelpers.ThrowIfNullOrNotInRange(
            _options.DefaultFileBufferSize, 1024, int.MaxValue, nameof(options) + "." + nameof(CompressionServiceOptions.DefaultFileBufferSize));

        ArgumentHelpers.ThrowIfNullOrNotInRange(_options.AsyncFileBufferSize, 1024, int.MaxValue, nameof(options) + "." + nameof(CompressionServiceOptions.AsyncFileBufferSize));
        ArgumentHelpers.ThrowIfNullOrNotInRange(_options.MaxInputSize, 1024, long.MaxValue, nameof(options) + "." + nameof(CompressionServiceOptions.MaxInputSize));

        // Last-writer-wins if two factories advertise the same algorithm; addons can therefore override built-in implementations.
        _factories = (factories ?? []).GroupBy(f => f.Algorithm).ToDictionary(g => g.Key, g => g.Last());
        Algorithm = _options.DefaultAlgorithm;
        _compressor = GetCompressor(Algorithm);
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _resolveResolver = resolveResolver;
        AlgorithmSelector = algorithmSelector;
    }

    /// <inheritdoc />
    public CompressionSelectionResult ResolveForCompress(CompressionSelectionContext context)
    {
        ArgumentHelpers.ThrowIfNull(context);
        if (AlgorithmSelector != null)
            return AlgorithmSelector.ResolveForCompress(context);

        return new(true, Algorithm);
    }

    /// <inheritdoc />
    public ICompressor GetCompressor(CompressionAlgorithm algorithm, CompressionLevel? level = null)
    {
        ArgumentHelpers.ThrowIfNull(algorithm);
        return _compressorCache.GetOrAdd((algorithm, level ?? _options.DefaultCompressionLevel), CreateCompressorInstance);
    }

    private ICompressor CreateCompressorInstance((CompressionAlgorithm Algorithm, CompressionLevel Level) key)
    {
        if (!_factories.TryGetValue(key.Algorithm, out var factory)) {
            throw new NotSupportedException(
                $"No compressor registered for algorithm '{key.Algorithm.Name}'. " + $"Did you forget to install/register the Lyo.Compression.{key.Algorithm.Name} addon package " +
                $"(e.g. services.Add{key.Algorithm.Name}Compressor())?");
        }

        return factory.Create(key.Level);
    }

    /// <inheritdoc />
    public CompressionInfo Compress(byte[] bytes, CompressionAlgorithm algorithm, out byte[] compressed, CompressionLevel? level = null)
        => CompressWithCompressor(bytes, GetCompressor(algorithm, level), algorithm.BinaryCompressMatchesStreamFormat, out compressed);

    /// <inheritdoc />
    public DecompressionInfo Decompress(byte[] compressedBytes, CompressionAlgorithm algorithm, out byte[] decompressed)
        => DecompressWithCompressor(compressedBytes, GetCompressor(algorithm), out decompressed);

    /// <inheritdoc />
    public void Compress(Stream inputStream, Stream outputStream, CompressionAlgorithm algorithm, CompressionLevel? level = null)
        => CompressWithCompressor(inputStream, outputStream, GetCompressor(algorithm, level));

    /// <inheritdoc />
    public void Decompress(Stream inputStream, Stream outputStream, CompressionAlgorithm algorithm)
        => DecompressWithCompressor(inputStream, outputStream, GetCompressor(algorithm));

    /// <inheritdoc />
    public Task CompressAsync(
        Stream inputStream,
        Stream outputStream,
        CompressionAlgorithm algorithm,
        int? chunkSize = null,
        CompressionLevel? level = null,
        CancellationToken ct = default)
        => CompressAsyncWithCompressor(inputStream, outputStream, GetCompressor(algorithm, level), ct);

    /// <inheritdoc />
    public Task DecompressAsync(Stream inputStream, Stream outputStream, CompressionAlgorithm algorithm, int? chunkSize = null, CancellationToken ct = default)
        => DecompressAsyncWithCompressor(inputStream, outputStream, GetCompressor(algorithm), ct);

    private string GetCompressedFilePath(string inputFilePath)
    {
        var extension = Path.GetExtension(inputFilePath);
        if (extension.Equals(FileExtension, StringComparison.OrdinalIgnoreCase))
            return inputFilePath;

        return Path.ChangeExtension(inputFilePath, FileExtension);
    }

    private string GetDecompressedFilePath(string inputFilePath)
    {
        var extension = Path.GetExtension(inputFilePath);
        if (!extension.Equals(FileExtension, StringComparison.OrdinalIgnoreCase))
            return inputFilePath;

        var pathWithoutExtension = Path.ChangeExtension(inputFilePath, null);
        return pathWithoutExtension;
    }

    private Stream CreateBufferedReadStream(string filePath)
    {
        // FileStream already has internal buffering, but BufferedStream adds an extra layer
        // which can help with small reads and reduces system calls.
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _options.DefaultFileBufferSize, false);
        return new BufferedStream(fileStream, _options.DefaultFileBufferSize);
    }

    private Stream CreateBufferedWriteStream(string filePath)
    {
        // Some compressors may check if output stream is readable, so use ReadWrite access
        // even though we only write to it. BufferedStream adds an extra buffering layer
        // which reduces system calls and improves performance.
        var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, _options.DefaultFileBufferSize, false);
        return new BufferedStream(fileStream, _options.DefaultFileBufferSize);
    }

    private FileStream CreateAsyncReadStream(string filePath) => new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _options.AsyncFileBufferSize, true);

    // Some compressors may check if output stream is readable, so use ReadWrite access
    // even though we only write to it.
    private FileStream CreateAsyncWriteStream(string filePath) => new(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, _options.AsyncFileBufferSize, true);

    /// <summary>
    /// Performs an atomic file operation by writing to a temporary file first, then atomically renaming it to the target file. This ensures that if the operation fails, no
    /// partial file is left at the target location.
    /// </summary>
    private static void AtomicFileOperation(string targetFilePath, Action<string> writeOperation)
    {
        // Use GUID-based temp file name to avoid conflicts with existing files
        var tempDir = Path.GetDirectoryName(targetFilePath);
        var tempFileName = Guid.NewGuid() + ".tmp";
        var tempFilePath = string.IsNullOrEmpty(tempDir) ? tempFileName : Path.Combine(tempDir, tempFileName);
        try {
            writeOperation(tempFilePath);
            MoveOverwrite(tempFilePath, targetFilePath);
        }
        catch {
            // Clean up temp file on failure
            if (File.Exists(tempFilePath)) {
                try {
                    File.Delete(tempFilePath);
                }
                catch {
                    // Ignore cleanup errors
                }
            }

            throw;
        }
    }

    /// <summary>Performs an atomic file operation asynchronously by writing to a temporary file first, then atomically renaming it to the target file.</summary>
    private static async Task AtomicFileOperationAsync(string targetFilePath, Func<string, CancellationToken, Task> writeOperationAsync, CancellationToken ct = default)
    {
        // Use GUID-based temp file name to avoid conflicts with existing files
        var tempDir = Path.GetDirectoryName(targetFilePath);
        var tempFileName = Guid.NewGuid() + ".tmp";
        var tempFilePath = string.IsNullOrEmpty(tempDir) ? tempFileName : Path.Combine(tempDir, tempFileName);
        try {
            await writeOperationAsync(tempFilePath, ct).ConfigureAwait(false);
            MoveOverwrite(tempFilePath, targetFilePath);
        }
        catch {
            if (!File.Exists(tempFilePath))
                throw;

            try {
                File.Delete(tempFilePath);
            }
            catch {
                // Ignore cleanup errors
            }

            throw;
        }
    }

    /// <summary>
    /// Replaces <paramref name="targetFilePath" /> with <paramref name="tempFilePath" /> in a single rename where the platform supports it. On net10+ this is
    /// <c>File.Move(…, overwrite: true)</c> (one atomic rename, no window where the target is missing); netstandard2.0 has no overwrite overload, so it falls back to delete-then-move.
    /// </summary>
    private static void MoveOverwrite(string tempFilePath, string targetFilePath)
    {
#if NETSTANDARD2_0
        if (File.Exists(targetFilePath))
            File.Delete(targetFilePath);

        File.Move(tempFilePath, targetFilePath);
#else
        File.Move(tempFilePath, targetFilePath, true);
#endif
    }

    private void ValidateInput(byte[]? bytes, string paramName) => ArgumentHelpers.ThrowIfNullOrNotInRange(bytes, 1, _options.MaxInputSize, paramName);

    private static void ValidateFilePath(string? filePath, string paramName)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(filePath);

        // Canonicalize path to prevent directory traversal attacks (e.g., ../../../etc/passwd)
        // This resolves relative paths and normalizes directory separators
        string canonicalPath;
        try {
            canonicalPath = Path.GetFullPath(filePath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
            throw new ArgumentException($"Invalid file path: {filePath}", paramName, ex);
        }

        var invalidChars = Path.GetInvalidPathChars();
        if (canonicalPath.IndexOfAny(invalidChars) >= 0)
            throw new ArgumentException($"File path contains invalid characters: {filePath}", paramName);
    }

    /// <summary>Gets an encoding by name, falling back to UTF-8 if the encoding name is invalid or not found.</summary>
    private static Encoding GetEncodingOrDefault(string encodingName)
    {
        try {
            return Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException) {
            // Invalid encoding name, fall back to UTF-8
            return Encoding.UTF8;
        }
    }

    /// <inheritdoc />
    public CompressionInfo Compress(byte[] bytes, out byte[] compressed, CompressionLevel? level = null)
        => CompressWithCompressor(bytes, level == null ? _compressor : GetCompressor(Algorithm, level), Algorithm.BinaryCompressMatchesStreamFormat, out compressed);

    private CompressionInfo CompressWithCompressor(byte[] bytes, ICompressor compressor, bool binaryCompressMatchesStreamFormat, out byte[] compressed)
    {
        using var timer = _metrics.StartTimer(Constants.Metrics.CompressDuration);
        ValidateInput(bytes, nameof(bytes));
        _logger.LogDebug("Compressing data of length {} ({Length})", FileSizeUnitInfo.FormatBestFitAbbreviation(bytes.Length), bytes.Length);
        var stopwatch = Stopwatch.StartNew();
        try {
            if (binaryCompressMatchesStreamFormat) {
                // Fast path: the binary API emits the same wire format the stream Decompress path reads, and for block codecs (LZ4/Zstd) it is a direct
                // buffer-to-buffer operation with exact-size output allocation.
                compressed = compressor.Compress(bytes);
            }
            else {
                // The codec's binary format differs from its stream format (e.g. Snappy raw block vs framed), so compress through the stream path to keep one
                // consistent wire format per algorithm. Presize the output near the worst case to avoid MemoryStream doubling-growth copies.
                using var inputStream = new MemoryStream(bytes, false);
                using var outputStream = new MemoryStream(bytes.Length + bytes.Length / 6 + 64);
                compressor.Compress(inputStream, outputStream);
                compressed = outputStream.ToArray();
            }

            stopwatch.Stop();
            var info = new CompressionInfo(bytes.Length, compressed.Length, stopwatch.ElapsedMilliseconds);
            _logger.LogDebug(
                "Compressed data to length {} ({Length}), ratio {CompressionRatio:P2}, saved {SpaceSavedPercent:P2}, time {TimeMs}ms",
                FileSizeUnitInfo.FormatBestFitAbbreviation(compressed.Length), compressed.Length, info.CompressionRatio, info.SpaceSavedPercent, info.TimeMs);

            _metrics.IncrementCounter(Constants.Metrics.CompressSuccess);
            _metrics.RecordGauge(Constants.Metrics.CompressRatio, info.CompressionRatio);
            _metrics.RecordGauge(Constants.Metrics.CompressInputSizeBytes, bytes.Length);
            _metrics.RecordGauge(Constants.Metrics.CompressOutputSizeBytes, compressed.Length);
            _metrics.RecordHistogram(Constants.Metrics.CompressDurationMs, stopwatch.ElapsedMilliseconds);
            return info;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            compressed = [];
            _logger.LogError(ex, "Failed to compress data of length {Length}: {Message}", bytes.Length, ex.Message);
            _metrics.IncrementCounter(Constants.Metrics.CompressFailure);
            _metrics.RecordError(Constants.Metrics.CompressDuration, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public DecompressionInfo Decompress(byte[] compressedBytes, out byte[] decompressed) => DecompressWithCompressor(compressedBytes, _compressor, out decompressed);

    private DecompressionInfo DecompressWithCompressor(byte[] compressedBytes, ICompressor compressor, out byte[] decompressed)
    {
        using var timer = _metrics.StartTimer(Constants.Metrics.DecompressDuration);
        ValidateInput(compressedBytes, nameof(compressedBytes));
        _logger.LogDebug("Decompressing data of length {Length}", compressedBytes.Length);
        var stopwatch = Stopwatch.StartNew();
        try {
            // Decompress through the stream path so MaxLengthStream stops a decompression bomb mid-flight, instead of materializing the full output and only then
            // checking its size. Presizing the output at the compressed length (output is almost always at least that big) skips most doubling-growth copies.
            using (var inputStream = new MemoryStream(compressedBytes, false)) {
                using var outputStream = new MemoryStream(compressedBytes.Length);
                compressor.Decompress(inputStream, new MaxLengthStream(outputStream, _options.MaxInputSize));
                decompressed = outputStream.ToArray();
            }

            stopwatch.Stop();
            var info = new DecompressionInfo(compressedBytes.Length, decompressed.Length, stopwatch.ElapsedMilliseconds);
            _logger.LogDebug(
                "Decompressed data to length {Length}, ratio {ExpansionRatio:P2}, increased {SizeIncreasePercent:P2}, time {TimeMs}ms", decompressed.Length, info.ExpansionRatio,
                info.SizeIncreasePercent, info.DecompressionTimeMs);

            _metrics.IncrementCounter(Constants.Metrics.DecompressSuccess);
            _metrics.RecordGauge(Constants.Metrics.DecompressInputSizeBytes, compressedBytes.Length);
            _metrics.RecordGauge(Constants.Metrics.DecompressOutputSizeBytes, decompressed.Length);
            _metrics.RecordHistogram(Constants.Metrics.DecompressDurationMs, stopwatch.ElapsedMilliseconds);
            return info;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            decompressed = [];
            _logger.LogError(ex, "Failed to decompress data of length {Length}: {Message}", compressedBytes.Length, ex.Message);
            _metrics.IncrementCounter(Constants.Metrics.DecompressFailure);
            _metrics.RecordError(Constants.Metrics.DecompressDuration, ex);
            throw;
        }
    }

    public void Compress(Stream inputStream, Stream outputStream, CompressionLevel? level = null)
        => CompressWithCompressor(inputStream, outputStream, level == null ? _compressor : GetCompressor(Algorithm, level));

    private void CompressWithCompressor(Stream inputStream, Stream outputStream, ICompressor compressor)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        _logger.LogDebug("Compressing stream input -> output");
        try {
            // Reset position if stream supports seeking
            if (inputStream.CanSeek && inputStream.Position != 0) {
                inputStream.Position = 0;
                _logger.LogDebug("Reset input stream position to 0");
            }

            compressor.Compress(inputStream, outputStream);
            _logger.LogDebug("Stream compression complete");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to compress stream: {Message}", ex.Message);
            throw;
        }
    }

    public void Decompress(Stream inputStream, Stream outputStream) => DecompressWithCompressor(inputStream, outputStream, _compressor);

    private void DecompressWithCompressor(Stream inputStream, Stream outputStream, ICompressor compressor)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        _logger.LogDebug("Decompressing stream input -> output");
        try {
            // Reset position if stream supports seeking
            if (inputStream.CanSeek && inputStream.Position != 0) {
                inputStream.Position = 0;
                _logger.LogDebug("Reset input stream position to 0");
            }

            // Guard against decompression bombs mid-flight (works for non-seekable outputs too, unlike an after-the-fact position check).
            var guardedOutput = new MaxLengthStream(outputStream, _options.MaxInputSize);
            compressor.Decompress(inputStream, guardedOutput);
            _logger.LogDebug("Stream decompression complete");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to decompress stream: {Message}", ex.Message);
            throw;
        }
    }

    public Task CompressAsync(Stream inputStream, Stream outputStream, int? chunkSize = null, CompressionLevel? level = null, CancellationToken ct = default)
        => CompressAsyncWithCompressor(inputStream, outputStream, level == null ? _compressor : GetCompressor(Algorithm, level), ct);

    private async Task CompressAsyncWithCompressor(Stream inputStream, Stream outputStream, ICompressor compressor, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        ct.ThrowIfCancellationRequested();
        _logger.LogDebug("Compressing stream asynchronously");
        try {
            // Reset position if stream supports seeking
            if (inputStream.CanSeek && inputStream.Position != 0) {
                inputStream.Position = 0;
                _logger.LogDebug("Reset input stream position to 0");
            }

            await compressor.CompressAsync(inputStream, outputStream, ct).ConfigureAwait(false);
            _logger.LogDebug("Async compression complete");
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Compression operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to compress stream asynchronously: {Message}", ex.Message);
            throw;
        }
    }

    public Task DecompressAsync(Stream inputStream, Stream outputStream, int? chunkSize = null, CancellationToken ct = default)
        => DecompressAsyncWithCompressor(inputStream, outputStream, _compressor, ct);

    private async Task DecompressAsyncWithCompressor(Stream inputStream, Stream outputStream, ICompressor compressor, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        ct.ThrowIfCancellationRequested();
        _logger.LogDebug("Decompressing stream asynchronously");
        try {
            // Reset position if stream supports seeking
            if (inputStream.CanSeek && inputStream.Position != 0) {
                inputStream.Position = 0;
                _logger.LogDebug("Reset input stream position to 0");
            }

            // Guard against decompression bombs mid-flight (works for non-seekable outputs too, unlike an after-the-fact position check).
            var guardedOutput = new MaxLengthStream(outputStream, _options.MaxInputSize);
            await compressor.DecompressAsync(inputStream, guardedOutput, ct).ConfigureAwait(false);
            _logger.LogDebug("Async decompression complete");
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("Decompression operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to decompress stream asynchronously: {Message}", ex.Message);
            throw;
        }
    }

    public CompressionInfo CompressString(string text, out byte[] compressed, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(text);
        encoding ??= GetEncodingOrDefault(_options.DefaultEncoding);
        _logger.LogDebug("Compressing string of length {Length} with encoding {Encoding}", text.Length, encoding.EncodingName);
        var bytes = encoding.GetBytes(text);
        return Compress(bytes, out compressed);
    }

    public DecompressionInfo DecompressString(byte[] compressedBytes, out string decompressed, Encoding? encoding = null)
    {
        encoding ??= GetEncodingOrDefault(_options.DefaultEncoding);
        _logger.LogDebug("Decompressing string from compressed bytes of length {Length} with encoding {Encoding}", compressedBytes.Length, encoding.EncodingName);
        var info = Decompress(compressedBytes, out var decompressedBytes);
        decompressed = encoding.GetString(decompressedBytes);
        return info;
    }

    public FileCompressionInfo CompressFile(string inputFilePath, string? outputFilePath = null)
    {
        using var timer = _metrics.StartTimer(Constants.Metrics.CompressFileDuration);
        ValidateFilePath(inputFilePath, nameof(inputFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(inputFilePath);
        outputFilePath ??= GetCompressedFilePath(inputFilePath);
        ValidateFilePath(outputFilePath, nameof(outputFilePath));

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        _logger.LogDebug("Compressing file {InputPath} to {OutputPath}", inputFilePath, outputFilePath);
        var stopwatch = Stopwatch.StartNew();
        var originalFileInfo = new FileInfo(inputFilePath);
        ArgumentHelpers.ThrowIfNullOrNotInRange(originalFileInfo.Length, 0, _options.MaxInputSize, nameof(originalFileInfo.Length));
        var originalSize = originalFileInfo.Length;
        try {
            AtomicFileOperation(
                outputFilePath, tempFilePath => {
                    using var inputStream = CreateBufferedReadStream(inputFilePath);
                    using var outputStream = CreateBufferedWriteStream(tempFilePath);
                    Compress(inputStream, outputStream);
                });

            stopwatch.Stop();
            var compressedFileInfo = new FileInfo(outputFilePath);
            var compressedSize = compressedFileInfo.Length;
            var info = new FileCompressionInfo(originalSize, compressedSize, stopwatch.ElapsedMilliseconds, inputFilePath, outputFilePath);
            _logger.LogDebug(
                "File compression complete, ratio {CompressionRatio:P2}, saved {SpaceSavedPercent:P2}, time {TimeMs}ms", info.CompressionRatio, info.SpaceSavedPercent,
                info.TimeMs);

            _metrics.IncrementCounter(Constants.Metrics.CompressFileSuccess);
            _metrics.RecordGauge(Constants.Metrics.CompressFileRatio, info.CompressionRatio);
            _metrics.RecordGauge(Constants.Metrics.CompressFileInputSizeBytes, originalSize);
            _metrics.RecordGauge(Constants.Metrics.CompressFileOutputSizeBytes, compressedSize);
            _metrics.RecordHistogram(Constants.Metrics.CompressFileDurationMs, stopwatch.ElapsedMilliseconds);
            return info;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to compress file {InputPath}: {Message}", inputFilePath, ex.Message);
            _metrics.IncrementCounter(Constants.Metrics.CompressFileFailure);
            _metrics.RecordError(Constants.Metrics.CompressFileDuration, ex);
            throw;
        }
    }

    public FileDecompressionInfo DecompressFile(string inputFilePath, string? outputFilePath = null)
    {
        using var timer = _metrics.StartTimer(Constants.Metrics.DecompressFileDuration);
        ValidateFilePath(inputFilePath, nameof(inputFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(inputFilePath);
        outputFilePath ??= GetDecompressedFilePath(inputFilePath);
        ValidateFilePath(outputFilePath, nameof(outputFilePath));

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        _logger.LogDebug("Decompressing file {InputPath} to {OutputPath}", inputFilePath, outputFilePath);
        var stopwatch = Stopwatch.StartNew();
        var compressedFileInfo = new FileInfo(inputFilePath);
        ArgumentHelpers.ThrowIfNullOrNotInRange(compressedFileInfo.Length, 0, _options.MaxInputSize, nameof(compressedFileInfo.Length));
        var compressedSize = compressedFileInfo.Length;
        try {
            AtomicFileOperation(
                outputFilePath, tempFilePath => {
                    using var inputStream = CreateBufferedReadStream(inputFilePath);
                    using var outputStream = CreateBufferedWriteStream(tempFilePath);
                    Decompress(inputStream, outputStream);
                });

            stopwatch.Stop();

            // The streaming bomb guard (MaxLengthStream) already enforced MaxInputSize during decompression.
            var decompressedFileInfo = new FileInfo(outputFilePath);
            var decompressedSize = decompressedFileInfo.Length;
            var info = new FileDecompressionInfo(compressedSize, decompressedSize, stopwatch.ElapsedMilliseconds, inputFilePath, outputFilePath);
            _logger.LogDebug(
                "File decompression complete, ratio {ExpansionRatio:P2}, increased {SizeIncreasePercent:P2}, time {TimeMs}ms", info.ExpansionRatio, info.SizeIncreasePercent,
                info.TimeMs);

            return info;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to decompress file {InputPath}: {Message}", inputFilePath, ex.Message);
            throw;
        }
    }

    public async Task<FileCompressionInfo> CompressFileAsync(string inputFilePath, string? outputFilePath = null, CancellationToken ct = default)
    {
        ValidateFilePath(inputFilePath, nameof(inputFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(inputFilePath);
        ct.ThrowIfCancellationRequested();
        outputFilePath ??= GetCompressedFilePath(inputFilePath);
        ValidateFilePath(outputFilePath, nameof(outputFilePath));

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        _logger.LogDebug("Compressing file asynchronously {InputPath} to {OutputPath}", inputFilePath, outputFilePath);
        var stopwatch = Stopwatch.StartNew();
        var originalFileInfo = new FileInfo(inputFilePath);
        ArgumentHelpers.ThrowIfNullOrNotInRange(originalFileInfo.Length, 0, _options.MaxInputSize, nameof(originalFileInfo.Length));
        var originalSize = originalFileInfo.Length;
        try {
            // Determine chunk size based on file size
            var chunkSize = StreamChunkSizeHelper.DetermineChunkSize(inputFilePath);
            await AtomicFileOperationAsync(
                    outputFilePath, async (tempFilePath, ct2) => {
#if NETSTANDARD2_0 && !NETSTANDARD2_1
                        using var inputStream = CreateAsyncReadStream(inputFilePath);
                        using var outputStream = CreateAsyncWriteStream(tempFilePath);
#else
                        await using var inputStream = CreateAsyncReadStream(inputFilePath);
                        await using var outputStream = CreateAsyncWriteStream(tempFilePath);
#endif
                        await CompressAsync(inputStream, outputStream, chunkSize, ct: ct2).ConfigureAwait(false);
                    }, ct)
                .ConfigureAwait(false);

            stopwatch.Stop();
            var compressedFileInfo = new FileInfo(outputFilePath);
            var compressedSize = compressedFileInfo.Length;
            var info = new FileCompressionInfo(originalSize, compressedSize, stopwatch.ElapsedMilliseconds, inputFilePath, outputFilePath);
            _logger.LogDebug(
                "Async file compression complete, ratio {CompressionRatio:P2}, saved {SpaceSavedPercent:P2}, time {TimeMs}ms", info.CompressionRatio, info.SpaceSavedPercent,
                info.TimeMs);

            return info;
        }
        catch (OperationCanceledException) {
            stopwatch.Stop();
            _logger.LogWarning("File compression operation was cancelled: {InputPath}", inputFilePath);
            throw;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to compress file asynchronously {InputPath}: {Message}", inputFilePath, ex.Message);
            throw;
        }
    }

    public async Task<FileDecompressionInfo> DecompressFileAsync(string inputFilePath, string? outputFilePath = null, CancellationToken ct = default)
    {
        ValidateFilePath(inputFilePath, nameof(inputFilePath));
        ArgumentHelpers.ThrowIfFileNotFound(inputFilePath);
        ct.ThrowIfCancellationRequested();
        outputFilePath ??= GetDecompressedFilePath(inputFilePath);
        ValidateFilePath(outputFilePath, nameof(outputFilePath));

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        _logger.LogDebug("Decompressing file asynchronously {InputPath} to {OutputPath}", inputFilePath, outputFilePath);
        var stopwatch = Stopwatch.StartNew();
        var compressedFileInfo = new FileInfo(inputFilePath);
        ArgumentHelpers.ThrowIfNullOrNotInRange(compressedFileInfo.Length, 0, _options.MaxInputSize, nameof(compressedFileInfo.Length));
        var compressedSize = compressedFileInfo.Length;
        try {
            // Determine chunk size based on file size
            var chunkSize = StreamChunkSizeHelper.DetermineChunkSize(inputFilePath);
            await AtomicFileOperationAsync(
                    outputFilePath, async (tempFilePath, ct2) => {
#if NETSTANDARD2_0 && !NETSTANDARD2_1
                        using var inputStream = CreateAsyncReadStream(inputFilePath);
                        using var outputStream = CreateAsyncWriteStream(tempFilePath);
#else
                        await using var inputStream = CreateAsyncReadStream(inputFilePath);
                        await using var outputStream = CreateAsyncWriteStream(tempFilePath);
#endif
                        await DecompressAsync(inputStream, outputStream, chunkSize, ct2).ConfigureAwait(false);
                    }, ct)
                .ConfigureAwait(false);

            stopwatch.Stop();

            // The streaming bomb guard (MaxLengthStream) already enforced MaxInputSize during decompression.
            var decompressedFileInfo = new FileInfo(outputFilePath);
            var decompressedSize = decompressedFileInfo.Length;
            var info = new FileDecompressionInfo(compressedSize, decompressedSize, stopwatch.ElapsedMilliseconds, inputFilePath, outputFilePath);
            _logger.LogDebug(
                "Async file decompression complete, ratio {ExpansionRatio:P2}, increased {SizeIncreasePercent:P2}, time {TimeMs}ms", info.ExpansionRatio, info.SizeIncreasePercent,
                info.TimeMs);

            return info;
        }
        catch (OperationCanceledException) {
            stopwatch.Stop();
            _logger.LogWarning("File decompression operation was cancelled: {InputPath}", inputFilePath);
            throw;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to decompress file asynchronously {InputPath}: {Message}", inputFilePath, ex.Message);
            throw;
        }
    }

    public double GetCompressionRatio(byte[] originalBytes, byte[] compressedBytes)
    {
        ArgumentHelpers.ThrowIfNull(originalBytes);
        ArgumentHelpers.ThrowIfNull(compressedBytes);
        if (originalBytes.Length == 0)
            return 0;

        var ratio = (double)compressedBytes.Length / originalBytes.Length;
        _logger.LogDebug("Compression ratio: {CompressionRatio:P2} ({CompressedLength}/{OriginalLength})", ratio, compressedBytes.Length, originalBytes.Length);
        return ratio;
    }

    public CompressionInfo CompressToBase64(byte[] bytes, out string base64String)
    {
        ValidateInput(bytes, nameof(bytes));
        _logger.LogDebug("Compressing and encoding to base64, input length {Length}", bytes.Length);
        var info = Compress(bytes, out var compressed);
        base64String = Convert.ToBase64String(compressed);
        return info;
    }

    public DecompressionInfo DecompressFromBase64(string base64String, out byte[] decompressed)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(base64String);
        _logger.LogDebug("Decoding from base64 and decompressing, base64 length {Length}", base64String.Length);
        byte[] compressedBytes;
        try {
            compressedBytes = Convert.FromBase64String(base64String);
        }
        catch (FormatException ex) {
            _logger.LogError(ex, "Invalid base64 string format");
            throw new ArgumentException("Invalid base64 string format", nameof(base64String), ex);
        }

        return Decompress(compressedBytes, out decompressed);
    }

    public bool TryCompress(byte[]? bytes, out byte[]? compressed, out CompressionInfo? info)
    {
        compressed = null;
        info = null;
        if (bytes == null || bytes.Length == 0) {
            _logger.LogWarning("Cannot compress null or empty data");
            return false;
        }

        try {
            info = Compress(bytes, out compressed);
            return true;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to compress data of length {Length}", bytes.Length);
            return false;
        }
    }

    public bool TryDecompress(byte[]? compressedBytes, out byte[]? decompressed, out DecompressionInfo? info)
    {
        decompressed = null;
        info = null;
        if (compressedBytes == null || compressedBytes.Length == 0) {
            _logger.LogWarning("Cannot decompress null or empty data");
            return false;
        }

        try {
            info = Decompress(compressedBytes, out decompressed);
            return true;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to decompress data of length {Length}", compressedBytes.Length);
            return false;
        }
    }

    // Known magic-number prefixes for compressed container formats, paired with the algorithm Name used by CompressionAlgorithm.TryFromName.
    // Note: raw Deflate and Brotli have no magic bytes and cannot be detected this way (the old 0x81-0x83 "Brotli" heuristic was bogus and matched arbitrary binary data).
    private static readonly (byte[] Magic, string AlgorithmName)[] MagicNumbers = [
        ([0x1F, 0x8B], "GZip"), // RFC 1952
        ([0x28, 0xB5, 0x2F, 0xFD], "ZstdSharp"), // Zstandard frame
        ([0x04, 0x22, 0x4D, 0x18], "LZ4"), // LZ4 frame
        ([0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00], "XZ"), // .xz container
        ([0x42, 0x5A, 0x68], "BZip2"), // "BZh"
        // ZLib (RFC 1950): 0x78 CMF + the four standard FLG values (level 1/fast/default/best), each keeping the (CMF<<8|FLG) % 31 == 0 invariant.
        ([0x78, 0x01], "ZLib"), ([0x78, 0x5E], "ZLib"), ([0x78, 0x9C], "ZLib"), ([0x78, 0xDA], "ZLib")
    ];

    public bool IsLikelyCompressed(byte[]? data)
    {
        if (data is null || data.Length < 2)
            return false;

        foreach (var (magic, _) in MagicNumbers) {
            if (StartsWith(data, magic))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryDetectAlgorithm(byte[]? data, out CompressionAlgorithm? algorithm)
    {
        algorithm = null;
        if (data is null || data.Length < 2)
            return false;

        foreach (var (magic, name) in MagicNumbers) {
            if (!StartsWith(data, magic))
                continue;

            // Detection only reports algorithms whose assemblies are loaded (addon algorithms self-register on load).
            algorithm = CompressionAlgorithm.TryFromName(name);
            return algorithm != null;
        }

        return false;
    }

    private static bool StartsWith(byte[] data, byte[] prefix)
    {
        if (data.Length < prefix.Length)
            return false;

        for (var i = 0; i < prefix.Length; i++) {
            if (data[i] != prefix[i])
                return false;
        }

        return true;
    }

    public Dictionary<string, byte[]> Compress(Dictionary<string, byte[]> items)
    {
        _logger.LogDebug("Compressing batch of {Count} items", items.Count);
        var results = new Dictionary<string, byte[]>(items.Count);
        try {
            foreach (var item in items) {
                _ = Compress(item.Value, out var compressed);
                results[item.Key] = compressed;
            }

            _logger.LogDebug("Batch compression complete for {Count} items", results.Count);
            return results;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to compress batch of {Count} items: {Message}", items.Count, ex.Message);
            throw;
        }
    }

    public Dictionary<string, byte[]> Decompress(Dictionary<string, byte[]> compressedItems)
    {
        _logger.LogDebug("Decompressing batch of {Count} items", compressedItems.Count);
        var results = new Dictionary<string, byte[]>(compressedItems.Count);
        try {
            foreach (var item in compressedItems) {
                _ = Decompress(item.Value, out var decompressed);
                results[item.Key] = decompressed;
            }

            _logger.LogDebug("Batch decompression complete for {Count} items", results.Count);
            return results;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to decompress batch of {Count} items: {Message}", compressedItems.Count, ex.Message);
            throw;
        }
    }

    public BatchFileCompressionResult CompressFiles(IEnumerable<string> filePaths)
    {
        var filePathsDict = filePaths.ToDictionary(path => path, _ => (string?)null);
        return CompressFiles(filePathsDict);
    }

    public BatchFileCompressionResult CompressFiles(Dictionary<string, string?> filePaths)
    {
        _logger.LogDebug("Compressing batch of {Count} files", filePaths.Count);
        var stopwatch = Stopwatch.StartNew();
        var successfulFiles = new List<FileCompressionInfo>();
        var failedFiles = new List<FailedFileOperation>();
        try {
            foreach (var filePath in filePaths) {
                try {
                    var info = CompressFile(filePath.Key, filePath.Value);
                    successfulFiles.Add(info);
                }
                catch (Exception ex) {
                    failedFiles.Add(new(filePath.Key, ex.Message));
                    _logger.LogWarning(ex, "Failed to compress file {InputPath}", filePath.Key);
                }
            }

            stopwatch.Stop();
            var result = new BatchFileCompressionResult(successfulFiles, failedFiles);
            _logger.LogDebug(
                "Batch file compression complete: {Successful} successful, {Failed} failed, ratio {Ratio:P2}", successfulFiles.Count, failedFiles.Count,
                result.AverageCompressionRatio);

            return result;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to compress batch of {Count} files: {Message}", filePaths.Count, ex.Message);
            throw;
        }
    }

    public BatchFileDecompressionResult DecompressFiles(IEnumerable<string> filePaths)
    {
        var filePathsDict = filePaths.ToDictionary(path => path, _ => (string?)null);
        return DecompressFiles(filePathsDict);
    }

    public BatchFileDecompressionResult DecompressFiles(Dictionary<string, string?> filePaths)
    {
        _logger.LogDebug("Decompressing batch of {Count} files", filePaths.Count);
        var stopwatch = Stopwatch.StartNew();
        var successfulFiles = new List<FileDecompressionInfo>();
        var failedFiles = new List<FailedFileOperation>();
        try {
            foreach (var filePath in filePaths) {
                try {
                    var info = DecompressFile(filePath.Key, filePath.Value);
                    successfulFiles.Add(info);
                }
                catch (Exception ex) {
                    failedFiles.Add(new(filePath.Key, ex.Message));
                    _logger.LogWarning(ex, "Failed to decompress file {InputPath}", filePath.Key);
                }
            }

            stopwatch.Stop();
            var result = new BatchFileDecompressionResult(successfulFiles, failedFiles);
            _logger.LogDebug(
                "Batch file decompression complete: {Successful} successful, {Failed} failed, ratio {Ratio:P2}", successfulFiles.Count, failedFiles.Count,
                result.AverageExpansionRatio);

            return result;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to decompress batch of {Count} files: {Message}", filePaths.Count, ex.Message);
            throw;
        }
    }

    public async Task<BatchFileCompressionResult> CompressFilesAsync(IEnumerable<string> filePaths, CancellationToken ct = default)
    {
        var filePathsDict = filePaths.ToDictionary(path => path, _ => (string?)null);
        return await CompressFilesAsync(filePathsDict, ct).ConfigureAwait(false);
    }

    public async Task<BatchFileCompressionResult> CompressFilesAsync(Dictionary<string, string?> filePaths, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogDebug("Compressing batch of {Count} files asynchronously with max parallelism {MaxParallel}", filePaths.Count, _options.MaxParallelFileOperations);
        var stopwatch = Stopwatch.StartNew();
#if NETSTANDARD2_0 && !NETSTANDARD2_1
        var successfulFiles = new ConcurrentBag<FileCompressionInfo>();
        var failedFiles = new ConcurrentBag<FailedFileOperation>();
        try {
            using var semaphore = new SemaphoreSlim(_options.MaxParallelFileOperations, _options.MaxParallelFileOperations);
            var tasks = new List<Task>(filePaths.Count);
            foreach (var filePath in filePaths) {
                ct.ThrowIfCancellationRequested();
                var inputPath = filePath.Key;
                var outputPath = filePath.Value;
                tasks.Add(ProcessCompressFileAsync(inputPath, outputPath, semaphore, ct, successfulFiles, failedFiles));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            stopwatch.Stop();
            var result = new BatchFileCompressionResult(successfulFiles.ToList(), failedFiles.ToList());
            _logger.LogDebug(
                "Batch async file compression complete: {Successful} successful, {Failed} failed, ratio {Ratio:P2}", successfulFiles.Count, failedFiles.Count,
                result.AverageCompressionRatio);

            return result;
        }
        catch (OperationCanceledException) {
            stopwatch.Stop();
            _logger.LogWarning("Batch file compression operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to compress batch of {Count} files asynchronously: {Message}", filePaths.Count, ex.Message);
            throw;
        }
#else
        var successfulFiles = new ConcurrentBag<FileCompressionInfo>();
        var failedFiles = new ConcurrentBag<FailedFileOperation>();
        try {
            using var semaphore = new SemaphoreSlim(_options.MaxParallelFileOperations, _options.MaxParallelFileOperations);
            var tasks = new List<Task>(filePaths.Count);
            foreach (var filePath in filePaths) {
                ct.ThrowIfCancellationRequested();
                var inputPath = filePath.Key;
                var outputPath = filePath.Value;
                tasks.Add(
                    ProcessCompressFileAsync(inputPath, outputPath, semaphore, ct)
                        .ContinueWith(
                            t => {
                                if (t.IsCompletedSuccessfully && t.Result != null)
                                    successfulFiles.Add(t.Result);
                                else if (t.IsFaulted) {
                                    var errorMessage = t.Exception?.GetBaseException().Message ?? "Unknown error";
                                    failedFiles.Add(new(inputPath, errorMessage));
                                }
                                else if (t.IsCanceled)
                                    failedFiles.Add(new(inputPath, "Operation was cancelled"));
                            }, TaskContinuationOptions.ExecuteSynchronously));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            stopwatch.Stop();
            var result = new BatchFileCompressionResult(successfulFiles.ToList(), failedFiles.ToList());
            _logger.LogDebug(
                "Batch async file compression complete: {Successful} successful, {Failed} failed, ratio {Ratio:P2}", successfulFiles.Count, failedFiles.Count,
                result.AverageCompressionRatio);

            return result;
        }
        catch (OperationCanceledException) {
            stopwatch.Stop();
            _logger.LogWarning("Batch file compression operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to compress batch of {Count} files asynchronously: {Message}", filePaths.Count, ex.Message);
            throw;
        }
#endif
    }

#if NETSTANDARD2_0 && !NETSTANDARD2_1
    private async Task ProcessCompressFileAsync(
        string inputPath,
        string? outputPath,
        SemaphoreSlim semaphore,
        CancellationToken ct,
        ConcurrentBag<FileCompressionInfo> successfulFiles,
        ConcurrentBag<FailedFileOperation> failedFiles)
    {
        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try {
            var info = await CompressFileAsync(inputPath, outputPath, ct).ConfigureAwait(false);
            successfulFiles.Add(info);
        }
        catch (Exception ex) {
            failedFiles.Add(new(inputPath, ex.Message));
            _logger.LogWarning(ex, "Failed to compress file {InputPath}", inputPath);
        }
        finally {
            semaphore.Release();
        }
    }
#else
    private async Task<FileCompressionInfo?> ProcessCompressFileAsync(string inputPath, string? outputPath, SemaphoreSlim semaphore, CancellationToken ct)
    {
        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try {
            return await CompressFileAsync(inputPath, outputPath, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to compress file {InputPath}", inputPath);
            throw;
        }
        finally {
            semaphore.Release();
        }
    }
#endif

    public async Task<BatchFileDecompressionResult> DecompressFilesAsync(IEnumerable<string> filePaths, CancellationToken ct = default)
    {
        var filePathsDict = filePaths.ToDictionary(path => path, _ => (string?)null);
        return await DecompressFilesAsync(filePathsDict, ct).ConfigureAwait(false);
    }

    public async Task<BatchFileDecompressionResult> DecompressFilesAsync(Dictionary<string, string?> filePaths, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogDebug("Decompressing batch of {Count} files asynchronously with max parallelism {MaxParallel}", filePaths.Count, _options.MaxParallelFileOperations);
        var stopwatch = Stopwatch.StartNew();
#if NETSTANDARD2_0 && !NETSTANDARD2_1
        var successfulFiles = new ConcurrentBag<FileDecompressionInfo>();
        var failedFiles = new ConcurrentBag<FailedFileOperation>();
        try {
            using var semaphore = new SemaphoreSlim(_options.MaxParallelFileOperations, _options.MaxParallelFileOperations);
            var tasks = new List<Task>(filePaths.Count);
            foreach (var filePath in filePaths) {
                ct.ThrowIfCancellationRequested();
                var inputPath = filePath.Key;
                var outputPath = filePath.Value;
                tasks.Add(ProcessDecompressFileAsync(inputPath, outputPath, semaphore, ct, successfulFiles, failedFiles));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            stopwatch.Stop();
            var result = new BatchFileDecompressionResult(successfulFiles.ToList(), failedFiles.ToList());
            _logger.LogDebug(
                "Batch async file decompression complete: {Successful} successful, {Failed} failed, ratio {Ratio:P2}", successfulFiles.Count, failedFiles.Count,
                result.AverageExpansionRatio);

            return result;
        }
        catch (OperationCanceledException) {
            stopwatch.Stop();
            _logger.LogWarning("Batch file decompression operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to decompress batch of {Count} files asynchronously: {Message}", filePaths.Count, ex.Message);
            throw;
        }
#else
        var successfulFiles = new ConcurrentBag<FileDecompressionInfo>();
        var failedFiles = new ConcurrentBag<FailedFileOperation>();
        try {
            using var semaphore = new SemaphoreSlim(_options.MaxParallelFileOperations, _options.MaxParallelFileOperations);
            var tasks = new List<Task>(filePaths.Count);
            foreach (var filePath in filePaths) {
                var inputPath = filePath.Key;
                var outputPath = filePath.Value;
                tasks.Add(
                    ProcessDecompressFileAsync(inputPath, outputPath, semaphore, ct)
                        .ContinueWith(
                            t => {
                                if (t.IsCompletedSuccessfully && t.Result != null)
                                    successfulFiles.Add(t.Result);
                                else if (t.IsFaulted) {
                                    var errorMessage = t.Exception?.GetBaseException().Message ?? "Unknown error";
                                    failedFiles.Add(new(inputPath, errorMessage));
                                }
                                else if (t.IsCanceled)
                                    failedFiles.Add(new(inputPath, "Operation was cancelled"));
                            }, TaskContinuationOptions.ExecuteSynchronously));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            stopwatch.Stop();
            var result = new BatchFileDecompressionResult(successfulFiles.ToList(), failedFiles.ToList());
            _logger.LogDebug(
                "Batch async file decompression complete: {Successful} successful, {Failed} failed, ratio {Ratio:P2}", successfulFiles.Count, failedFiles.Count,
                result.AverageExpansionRatio);

            return result;
        }
        catch (OperationCanceledException) {
            stopwatch.Stop();
            _logger.LogWarning("Batch file decompression operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to decompress batch of {Count} files asynchronously: {Message}", filePaths.Count, ex.Message);
            throw;
        }
#endif
    }

#if NETSTANDARD2_0 && !NETSTANDARD2_1
    private async Task ProcessDecompressFileAsync(
        string inputPath,
        string? outputPath,
        SemaphoreSlim semaphore,
        CancellationToken ct,
        ConcurrentBag<FileDecompressionInfo> successfulFiles,
        ConcurrentBag<FailedFileOperation> failedFiles)
    {
        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try {
            var info = await DecompressFileAsync(inputPath, outputPath, ct).ConfigureAwait(false);
            successfulFiles.Add(info);
        }
        catch (Exception ex) {
            failedFiles.Add(new(inputPath, ex.Message));
            _logger.LogWarning(ex, "Failed to decompress file {InputPath}", inputPath);
        }
        finally {
            semaphore.Release();
        }
    }
#else
    private async Task<FileDecompressionInfo?> ProcessDecompressFileAsync(string inputPath, string? outputPath, SemaphoreSlim semaphore, CancellationToken ct)
    {
        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try {
            return await DecompressFileAsync(inputPath, outputPath, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to decompress file {InputPath}", inputPath);
            throw;
        }
        finally {
            semaphore.Release();
        }
    }
#endif

    public async Task CompressStringToStreamAsync(string text, Stream outputStream, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        ct.ThrowIfCancellationRequested();
        encoding ??= GetEncodingOrDefault(_options.DefaultEncoding);
        _logger.LogDebug("Compressing string to stream, length {Length} with encoding {Encoding}", text.Length, encoding.EncodingName);
        try {
            var bytes = encoding.GetBytes(text);
            // Determine chunk size based on string data size
            var chunkSize = StreamChunkSizeHelper.DetermineChunkSize(bytes.Length);
            using var inputStream = new MemoryStream(bytes);
            await CompressAsync(inputStream, outputStream, chunkSize, ct: ct).ConfigureAwait(false);
            _logger.LogDebug("String compression to stream complete");
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("String compression to stream operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to compress string to stream: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<string> DecompressStringFromStreamAsync(Stream inputStream, Encoding? encoding = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        ct.ThrowIfCancellationRequested();
        encoding ??= GetEncodingOrDefault(_options.DefaultEncoding);
        _logger.LogDebug("Decompressing string from stream with encoding {Encoding}", encoding.EncodingName);
        try {
            // Reset position if stream supports seeking
            if (inputStream.CanSeek && inputStream.Position != 0) {
                inputStream.Position = 0;
                _logger.LogDebug("Reset input stream position to 0");
            }

            // Determine chunk size based on stream size if available
            var chunkSize = StreamChunkSizeHelper.DetermineChunkSize(inputStream);
            using var outputStream = new MemoryStream();
            await DecompressAsync(inputStream, outputStream, chunkSize, ct).ConfigureAwait(false);
            outputStream.Position = 0;
            // Optimize memory usage: since we control the MemoryStream creation,
            // we can use GetBuffer() to avoid unnecessary copy, but we must respect Length
            // to avoid reading beyond actual data
            var length = (int)outputStream.Length;

            // MemoryStream.GetBuffer() returns the underlying buffer which may be larger than Length
            // This avoids the copy that ToArray() would create
            // Use GetString with offset and count to avoid extra allocations (compatible with .NET Standard 2.0)
            if (outputStream is MemoryStream memStream) {
                var buffer = memStream.GetBuffer();
                // Use GetString(byte[], int, int) which is available in .NET Standard 2.0
                // This still avoids the ToArray() copy while maintaining compatibility
                var result = encoding.GetString(buffer, 0, length);
                _logger.LogDebug("String decompression from stream complete, length {Length}", result.Length);
                return result;
            }

            // Fallback for non-MemoryStream (shouldn't happen, but defensive)
            var fallbackBuffer = outputStream.ToArray();
            var fallbackResult = encoding.GetString(fallbackBuffer);
            _logger.LogDebug("String decompression from stream complete, length {Length}", fallbackResult.Length);
            return fallbackResult;
        }
        catch (OperationCanceledException) {
            _logger.LogWarning("String decompression from stream operation was cancelled");
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to decompress string from stream: {Message}", ex.Message);
            throw;
        }
    }
}