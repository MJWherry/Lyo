using Lyo.Result;

namespace Lyo.Media.Models;

/// <summary>
/// Converts, transcodes, and compresses video. Does not convert audio-only jobs; use <see cref="IAudioConverter" />. Images stay on IImageService.
/// Implementations must allow concurrent convert calls on one instance (one process per call, no process-wide lock).
/// </summary>
public interface IVideoConverter
{
    /// <summary>Converts using the paths and knobs on <paramref name="request" />.</summary>
    Task<Result<bool>> ConvertAsync(VideoConversionRequest request, CancellationToken ct = default);

    /// <summary>Converts a file or URL into another file on disk.</summary>
    Task<Result<bool>> ConvertFileToFileAsync(string inputPath, string outputPath, VideoConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts a file or URL and writes the result to a stream. Use <see cref="MediaIoMode.Pipe" /> so the write is incremental.</summary>
    Task<Result<bool>> ConvertFileToStreamAsync(string inputPath, Stream outputStream, VideoConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts a file or URL and returns the result as bytes. For tiny clips only; prefer a stream or <see cref="StartConvertAsync(string, VideoConversionOptions, CancellationToken)" /> for library media.</summary>
    Task<Result<byte[]>> ConvertFileToBytesAsync(string inputPath, VideoConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from an input stream into an output stream.</summary>
    Task<Result<bool>> ConvertStreamToStreamAsync(Stream inputStream, Stream outputStream, VideoConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from an input stream into a file on disk.</summary>
    Task<Result<bool>> ConvertStreamToFileAsync(Stream inputStream, string outputPath, VideoConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts from an input stream and returns bytes. For tiny clips only.</summary>
    Task<Result<byte[]>> ConvertStreamToBytesAsync(Stream inputStream, VideoConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts bytes and returns bytes. For tiny clips only.</summary>
    Task<Result<byte[]>> ConvertBytesToBytesAsync(byte[] inputBytes, VideoConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts bytes and writes the result to a stream.</summary>
    Task<Result<bool>> ConvertBytesToStreamAsync(byte[] inputBytes, Stream outputStream, VideoConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>Converts bytes into a file on disk.</summary>
    Task<Result<bool>> ConvertBytesToFileAsync(byte[] inputBytes, string outputPath, VideoConversionOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Starts a convert with output on a pipe and returns as soon as stdout is hooked. Read <see cref="IMediaProcessSession.StandardOutput" /> while the process runs.
    /// Dispose the session to cancel. Does not apply a host process timeout.
    /// </summary>
    Task<Result<IMediaProcessSession>> StartConvertAsync(string inputPathOrUrl, VideoConversionOptions options, CancellationToken ct = default);

    /// <summary>Starts a convert with stdin from <paramref name="input" /> and stdout on a readable session stream.</summary>
    Task<Result<IMediaProcessSession>> StartConvertAsync(Stream input, VideoConversionOptions options, CancellationToken ct = default);

    /// <summary>Writes one video frame to an image file. Default <paramref name="at" /> is the start of the file.</summary>
    Task<Result<bool>> ExtractFrameAsync(string inputPath, string outputPath, TimeSpan? at = null, VideoConversionOptions? options = null, CancellationToken ct = default);
}
