using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FFmpeg;

/// <summary><see cref="IVideoConverter" /> backed by ffmpeg. Concurrent convert calls on one instance are safe (one process per call).</summary>
public sealed class FFmpegVideoConverter : IVideoConverter
{
    private readonly FFmpegConvertEngine _engine;

    /// <summary>Creates a converter. Metrics are recorded only when <see cref="FFmpegOptions.EnableMetrics" /> is on and <paramref name="metrics" /> is provided.</summary>
    public FFmpegVideoConverter(FFmpegOptions? options = null, ILogger<FFmpegVideoConverter>? logger = null, IMetrics? metrics = null)
    {
        var opts = options ?? new FFmpegOptions();
        _engine = new(opts, logger ?? NullLogger<FFmpegVideoConverter>.Instance, metrics);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ConvertAsync(VideoConversionRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        request.Validate();
        return await ConvertFileToFileAsync(request.InputPath, request.OutputPath, request.Options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<bool>> ConvertFileToFileAsync(string inputPath, string outputPath, VideoConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertFileToFileAsync(inputPath, outputPath, Prepare(options, fileToFile: true), ct);

    /// <inheritdoc />
    public Task<Result<bool>> ConvertFileToStreamAsync(string inputPath, Stream outputStream, VideoConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertFileToStreamAsync(inputPath, outputStream, Prepare(options, fileToFile: false), ct);

    /// <inheritdoc />
    public Task<Result<byte[]>> ConvertFileToBytesAsync(string inputPath, VideoConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertFileToBytesAsync(inputPath, Prepare(options, fileToFile: false), ct);

    /// <inheritdoc />
    public Task<Result<bool>> ConvertStreamToStreamAsync(Stream inputStream, Stream outputStream, VideoConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertStreamToStreamAsync(inputStream, outputStream, Prepare(options, fileToFile: false), ct);

    /// <inheritdoc />
    public Task<Result<bool>> ConvertStreamToFileAsync(Stream inputStream, string outputPath, VideoConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertStreamToFileAsync(inputStream, outputPath, Prepare(options, fileToFile: false), ct);

    /// <inheritdoc />
    public Task<Result<byte[]>> ConvertStreamToBytesAsync(Stream inputStream, VideoConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertStreamToBytesAsync(inputStream, Prepare(options, fileToFile: false), ct);

    /// <inheritdoc />
    public Task<Result<byte[]>> ConvertBytesToBytesAsync(byte[] inputBytes, VideoConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputBytes);
        return ConvertStreamToBytesAsync(new MemoryStream(inputBytes, writable: false), options, ct);
    }

    /// <inheritdoc />
    public Task<Result<bool>> ConvertBytesToStreamAsync(byte[] inputBytes, Stream outputStream, VideoConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputBytes);
        return ConvertStreamToStreamAsync(new MemoryStream(inputBytes, writable: false), outputStream, options, ct);
    }

    /// <inheritdoc />
    public Task<Result<bool>> ConvertBytesToFileAsync(byte[] inputBytes, string outputPath, VideoConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputBytes);
        return ConvertStreamToFileAsync(new MemoryStream(inputBytes, writable: false), outputPath, options, ct);
    }

    /// <inheritdoc />
    public Task<Result<IMediaProcessSession>> StartConvertAsync(string inputPathOrUrl, VideoConversionOptions options, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        return _engine.StartConvertAsync(inputPathOrUrl, ConvertKnobs.FromVideo(options), ct);
    }

    /// <inheritdoc />
    public Task<Result<IMediaProcessSession>> StartConvertAsync(Stream input, VideoConversionOptions options, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        return _engine.StartConvertAsync(input, ConvertKnobs.FromVideo(options), ct);
    }

    /// <inheritdoc />
    public Task<Result<bool>> ExtractFrameAsync(string inputPath, string outputPath, TimeSpan? at = null, VideoConversionOptions? options = null, CancellationToken ct = default)
    {
        var opts = options ?? new VideoConversionOptions();
        opts.Validate();
        return _engine.ExtractFrameAsync(inputPath, outputPath, at, ConvertKnobs.FromVideo(opts), ct);
    }

    private static ConvertKnobs Prepare(VideoConversionOptions? options, bool fileToFile)
    {
        var opts = options ?? new VideoConversionOptions();
        opts.Validate();
        var knobs = ConvertKnobs.FromVideo(opts);
        FFmpegConvertEngine.ThrowIfFileToFilePipe(knobs, fileToFile);
        return knobs;
    }
}
