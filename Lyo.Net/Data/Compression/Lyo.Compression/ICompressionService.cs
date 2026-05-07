using System.Text;
using Lyo.Compression.Models;

namespace Lyo.Compression;

/// <summary>Compress and decompress payloads using the algorithm bound to the implementing service instance.</summary>
/// <remarks>
/// <para>
/// Implementations are typically registered as singletons. File outputs use atomic write-to-temp-then-rename where applicable. Respect
/// <see cref="Models.CompressionServiceOptions.MaxInputSize" /> for both compressed input size and maximum decompressed output size.
/// </para>
/// </remarks>
public interface ICompressionService
{
    /// <summary>File extension for this instance's algorithm (from <see cref="Constants.Data.AlgorithmExtensions" />).</summary>
    string FileExtension { get; }

    /// <summary>Algorithm selected when the service was constructed.</summary>
    CompressionAlgorithm Algorithm { get; }

    /// <summary>Compresses a byte array in memory.</summary>
    /// <param name="bytes">Uncompressed input; must be non-empty and within configured max size.</param>
    /// <param name="compressed">Compressed output buffer.</param>
    /// <returns>Timing and size metadata.</returns>
    CompressionInfo Compress(byte[] bytes, out byte[] compressed);

    /// <summary>Decompresses a byte array produced by the same algorithm.</summary>
    /// <param name="compressedBytes">Compressed input.</param>
    /// <param name="decompressed">Restored payload.</param>
    /// <returns>Timing and size metadata.</returns>
    DecompressionInfo Decompress(byte[] compressedBytes, out byte[] decompressed);

    /// <summary>Compresses all bytes read from <paramref name="inputStream" /> into <paramref name="outputStream" />. Does not dispose streams.</summary>
    void Compress(Stream inputStream, Stream outputStream);

    /// <summary>Decompresses from <paramref name="inputStream" /> into <paramref name="outputStream" />. Does not dispose streams.</summary>
    void Decompress(Stream inputStream, Stream outputStream);

    /// <summary>Async stream compress; optional <paramref name="chunkSize" /> controls read buffer sizing.</summary>
    Task CompressAsync(Stream inputStream, Stream outputStream, int? chunkSize = null, CancellationToken ct = default);

    /// <summary>Async stream decompress.</summary>
    Task DecompressAsync(Stream inputStream, Stream outputStream, int? chunkSize = null, CancellationToken ct = default);

    /// <summary>Encodes <paramref name="text" /> with <paramref name="encoding" /> (or default from options), then compresses.</summary>
    CompressionInfo CompressString(string text, out byte[] compressed, Encoding? encoding = null);

    /// <summary>Decompresses then decodes to string using <paramref name="encoding" /> or default.</summary>
    DecompressionInfo DecompressString(byte[] compressedBytes, out string decompressed, Encoding? encoding = null);

    /// <summary>Writes compressed bytes for <paramref name="text" /> directly to <paramref name="outputStream" />.</summary>
    Task CompressStringToStreamAsync(string text, Stream outputStream, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Reads compressed stream content and returns the decoded string.</summary>
    Task<string> DecompressStringFromStreamAsync(Stream inputStream, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>
    /// Compresses a file on disk. When <paramref name="outputFilePath" /> is null, output path is derived from <paramref name="inputFilePath" /> and <see cref="FileExtension" />
    /// .
    /// </summary>
    FileCompressionInfo CompressFile(string inputFilePath, string? outputFilePath = null);

    /// <summary>Decompresses a file; optional explicit output path, otherwise derived by stripping <see cref="FileExtension" />.</summary>
    FileDecompressionInfo DecompressFile(string inputFilePath, string? outputFilePath = null);

    /// <summary>Async variant of <see cref="CompressFile" />.</summary>
    Task<FileCompressionInfo> CompressFileAsync(string inputFilePath, string? outputFilePath = null, CancellationToken ct = default);

    /// <summary>Async variant of <see cref="DecompressFile" />.</summary>
    Task<FileDecompressionInfo> DecompressFileAsync(string inputFilePath, string? outputFilePath = null, CancellationToken ct = default);

    /// <summary>Compresses then returns Base64 text suitable for transport in JSON or logs.</summary>
    CompressionInfo CompressToBase64(byte[] bytes, out string base64String);

    /// <summary>Parses Base64 then decompresses.</summary>
    DecompressionInfo DecompressFromBase64(string base64String, out byte[] decompressed);

    /// <summary>Non-throwing compress; returns <see langword="false" /> on validation or compressor failure.</summary>
    bool TryCompress(byte[]? bytes, out byte[]? compressed, out CompressionInfo? info);

    /// <summary>Non-throwing decompress; returns <see langword="false" /> on validation or decompressor failure.</summary>
    bool TryDecompress(byte[]? compressedBytes, out byte[]? decompressed, out DecompressionInfo? info);

    /// <summary>Compressed size divided by original size (0–1+); convenience over metadata types.</summary>
    double GetCompressionRatio(byte[] originalBytes, byte[] compressedBytes);

    /// <summary>Heuristic check for common compressed magic bytes (e.g. GZip, zlib wrapper, Brotli-ish leading byte). Not a guarantee — treat as a hint only.</summary>
    bool IsLikelyCompressed(byte[] data);

    /// <summary>Compresses each dictionary entry in parallel; keys are preserved.</summary>
    Dictionary<string, byte[]> Compress(Dictionary<string, byte[]> items);

    /// <summary>Decompresses each dictionary entry in parallel.</summary>
    Dictionary<string, byte[]> Decompress(Dictionary<string, byte[]> compressedItems);

    /// <summary>Compresses each path; failures collected in <see cref="BatchFileCompressionResult.FailedFiles" />.</summary>
    BatchFileCompressionResult CompressFiles(IEnumerable<string> filePaths);

    /// <summary>Compresses each mapping: key = input path, value = output path or null for default output path.</summary>
    BatchFileCompressionResult CompressFiles(Dictionary<string, string?> filePaths);

    /// <summary>Batch decompress for an enumerable of compressed file paths.</summary>
    BatchFileDecompressionResult DecompressFiles(IEnumerable<string> filePaths);

    /// <summary>Batch decompress with per-file output path mapping.</summary>
    BatchFileDecompressionResult DecompressFiles(Dictionary<string, string?> filePaths);

    /// <summary>Async batch compress.</summary>
    Task<BatchFileCompressionResult> CompressFilesAsync(IEnumerable<string> filePaths, CancellationToken ct = default);

    /// <summary>Async batch compress with explicit output paths.</summary>
    Task<BatchFileCompressionResult> CompressFilesAsync(Dictionary<string, string?> filePaths, CancellationToken ct = default);

    /// <summary>Async batch decompress.</summary>
    Task<BatchFileDecompressionResult> DecompressFilesAsync(IEnumerable<string> filePaths, CancellationToken ct = default);

    /// <summary>Async batch decompress with explicit output paths.</summary>
    Task<BatchFileDecompressionResult> DecompressFilesAsync(Dictionary<string, string?> filePaths, CancellationToken ct = default);
}