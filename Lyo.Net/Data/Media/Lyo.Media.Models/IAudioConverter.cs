using Lyo.Result;

namespace Lyo.Media.Models;

/// <summary>
/// Converts and compresses audio. Does not convert video; use <see cref="IVideoConverter" />. Images stay on IImageService.
/// Implementations must allow concurrent convert calls on one instance (one process per call, no process-wide lock).
/// </summary>
public interface IAudioConverter
{
    /// <summary>Converts using the paths and knobs on <paramref name="request" />.</summary>
    Task<Result<bool>> ConvertAsync(AudioConversionRequest request, CancellationToken ct = default);

    /// <summary>Converts a file or URL into another file on disk.</summary>
    Task<Result<bool>> ConvertFileToFileAsync(string inputPath, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts a file or URL and writes the result to a stream. Use <see cref="MediaIoMode.Pipe" /> so the write is incremental.</summary>
    Task<Result<bool>> ConvertFileToStreamAsync(string inputPath, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts a file or URL and returns the result as bytes. For tiny clips only; prefer a stream or <see cref="StartConvertAsync(string, AudioConversionOptions, CancellationToken)" /> for library media.</summary>
    Task<Result<byte[]>> ConvertFileToBytesAsync(string inputPath, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from an input stream into an output stream.</summary>
    Task<Result<bool>> ConvertStreamToStreamAsync(Stream inputStream, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from an input stream into a file on disk.</summary>
    Task<Result<bool>> ConvertStreamToFileAsync(Stream inputStream, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from an input stream and returns bytes. For tiny clips only.</summary>
    Task<Result<byte[]>> ConvertStreamToBytesAsync(Stream inputStream, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts bytes and returns bytes. For tiny clips only.</summary>
    Task<Result<byte[]>> ConvertBytesToBytesAsync(byte[] inputBytes, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts bytes and writes the result to a stream.</summary>
    Task<Result<bool>> ConvertBytesToStreamAsync(byte[] inputBytes, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts bytes into a file on disk.</summary>
    Task<Result<bool>> ConvertBytesToFileAsync(byte[] inputBytes, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Starts a convert with output on a pipe and returns as soon as stdout is hooked. Read <see cref="IMediaProcessSession.StandardOutput" /> while the process runs.
    /// Dispose the session to cancel. Does not apply a host process timeout.
    /// </summary>
    Task<Result<IMediaProcessSession>> StartConvertAsync(string inputPathOrUrl, AudioConversionOptions options, CancellationToken ct = default);

    /// <summary>Starts a convert with stdin from <paramref name="input" /> and stdout on a readable session stream.</summary>
    Task<Result<IMediaProcessSession>> StartConvertAsync(Stream input, AudioConversionOptions options, CancellationToken ct = default);

    /// <summary>Starts a raw PCM s16le session. Equivalent to <see cref="StartConvertAsync(string, AudioConversionOptions, CancellationToken)" /> with <see cref="AudioConversionOptions.ForRawPcm" />.</summary>
    Task<Result<IMediaProcessSession>> StartRawPcmAsync(string inputPathOrUrl, int sampleRate = 48000, int channels = 2, CancellationToken ct = default);
}
