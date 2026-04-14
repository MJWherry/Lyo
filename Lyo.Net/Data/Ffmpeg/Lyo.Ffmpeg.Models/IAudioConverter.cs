using Lyo.Common;

namespace Lyo.Ffmpeg.Models;

/// <summary>Service interface for converting audio between formats.</summary>
public interface IAudioConverter
{
    /// <summary>Converts an audio file to a different format.</summary>
    Task<Result<bool>> ConvertAsync(AudioConversionRequest request, CancellationToken ct = default);

    /// <summary>Converts from file path to file path.</summary>
    Task<Result<bool>> ConvertFileToFileAsync(string inputPath, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from file path to output stream.</summary>
    Task<Result<bool>> ConvertFileToStreamAsync(string inputPath, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from file path to byte array.</summary>
    Task<Result<byte[]>> ConvertFileToBytesAsync(string inputPath, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from input stream to output stream.</summary>
    Task<Result<bool>> ConvertStreamToStreamAsync(Stream inputStream, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from input stream to file path.</summary>
    Task<Result<bool>> ConvertStreamToFileAsync(Stream inputStream, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from input stream to byte array.</summary>
    Task<Result<byte[]>> ConvertStreamToBytesAsync(Stream inputStream, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from byte array to byte array.</summary>
    Task<Result<byte[]>> ConvertBytesToBytesAsync(byte[] inputBytes, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from byte array to output stream.</summary>
    Task<Result<bool>> ConvertBytesToStreamAsync(byte[] inputBytes, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from byte array to file path.</summary>
    Task<Result<bool>> ConvertBytesToFileAsync(byte[] inputBytes, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default);
}