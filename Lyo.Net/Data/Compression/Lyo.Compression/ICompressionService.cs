using System.Text;
using Lyo.Compression.Models;

namespace Lyo.Compression;

/// <summary>Service for compressing and decompressing data using various algorithms (GZip, Brotli, etc.).</summary>
public interface ICompressionService
{
    /// <summary>Gets the file extension used for compressed files (e.g. ".gz", ".br").</summary>
    string FileExtension { get; }

    /// <summary>Gets the compression algorithm used by this service.</summary>
    CompressionAlgorithm Algorithm { get; }

    /// <summary>Compresses byte array data.</summary>
    /// <param name="bytes">The data to compress.</param>
    /// <param name="compressed">The compressed output.</param>
    /// <returns>Compression metadata (original size, compressed size, ratio).</returns>
    CompressionInfo Compress(byte[] bytes, out byte[] compressed);

    /// <summary>Decompresses byte array data.</summary>
    /// <param name="compressedBytes">The compressed data.</param>
    /// <param name="decompressed">The decompressed output.</param>
    /// <returns>Decompression metadata.</returns>
    DecompressionInfo Decompress(byte[] compressedBytes, out byte[] decompressed);

    // Stream compression/decompression
    void Compress(Stream inputStream, Stream outputStream);

    void Decompress(Stream inputStream, Stream outputStream);

    Task CompressAsync(Stream inputStream, Stream outputStream, int? chunkSize = null, CancellationToken ct = default);

    Task DecompressAsync(Stream inputStream, Stream outputStream, int? chunkSize = null, CancellationToken ct = default);

    // String compression/decompression
    CompressionInfo CompressString(string text, out byte[] compressed, Encoding? encoding = null);

    DecompressionInfo DecompressString(byte[] compressedBytes, out string decompressed, Encoding? encoding = null);

    Task CompressStringToStreamAsync(string text, Stream outputStream, Encoding? encoding = null, CancellationToken ct = default);

    Task<string> DecompressStringFromStreamAsync(Stream inputStream, Encoding? encoding = null, CancellationToken ct = default);

    // File compression/decompression
    FileCompressionInfo CompressFile(string inputFilePath, string? outputFilePath = null);

    FileDecompressionInfo DecompressFile(string inputFilePath, string? outputFilePath = null);

    Task<FileCompressionInfo> CompressFileAsync(string inputFilePath, string? outputFilePath = null, CancellationToken ct = default);

    Task<FileDecompressionInfo> DecompressFileAsync(string inputFilePath, string? outputFilePath = null, CancellationToken ct = default);

    // Base64 compression/decompression
    CompressionInfo CompressToBase64(byte[] bytes, out string base64String);

    DecompressionInfo DecompressFromBase64(string base64String, out byte[] decompressed);

    // Try methods (non-throwing)
    bool TryCompress(byte[] bytes, out byte[]? compressed, out CompressionInfo? info);

    bool TryDecompress(byte[] compressedBytes, out byte[]? decompressed, out DecompressionInfo? info);

    // Utility methods
    double GetCompressionRatio(byte[] originalBytes, byte[] compressedBytes);

    bool IsLikelyCompressed(byte[] data);

    // Batch compression/decompression (byte arrays)
    Dictionary<string, byte[]> Compress(Dictionary<string, byte[]> items);

    Dictionary<string, byte[]> Decompress(Dictionary<string, byte[]> compressedItems);

    // Batch file compression/decompression
    BatchFileCompressionResult CompressFiles(IEnumerable<string> filePaths);

    BatchFileCompressionResult CompressFiles(Dictionary<string, string?> filePaths);

    BatchFileDecompressionResult DecompressFiles(IEnumerable<string> filePaths);

    BatchFileDecompressionResult DecompressFiles(Dictionary<string, string?> filePaths);

    Task<BatchFileCompressionResult> CompressFilesAsync(IEnumerable<string> filePaths, CancellationToken ct = default);

    Task<BatchFileCompressionResult> CompressFilesAsync(Dictionary<string, string?> filePaths, CancellationToken ct = default);

    Task<BatchFileDecompressionResult> DecompressFilesAsync(IEnumerable<string> filePaths, CancellationToken ct = default);

    Task<BatchFileDecompressionResult> DecompressFilesAsync(Dictionary<string, string?> filePaths, CancellationToken ct = default);
}