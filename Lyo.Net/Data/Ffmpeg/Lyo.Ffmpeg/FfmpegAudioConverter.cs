using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Ffmpeg.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Ffmpeg;

/// <summary>FFmpeg-based implementation of IAudioConverter.</summary>
public sealed class FfmpegAudioConverter : IAudioConverter
{
    private readonly ILogger<FfmpegAudioConverter> _logger;
    private readonly IMetrics _metrics;
    private readonly FfmpegOptions _options;
    private readonly FfmpegProcessRunner _runner;

    public FfmpegAudioConverter(FfmpegOptions? options = null, ILogger<FfmpegAudioConverter>? logger = null, IMetrics? metrics = null)
    {
        _options = options ?? new FfmpegOptions();
        _logger = logger ?? NullLogger<FfmpegAudioConverter>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _runner = new(_options, _logger);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ConvertAsync(AudioConversionRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
        return await ConvertFileToFileAsync(request.InputPath, request.OutputPath, ToOptions(request), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ConvertFileToFileAsync(string inputPath, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath, nameof(inputPath));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));
        using var timer = _metrics.StartTimer(Constants.Metrics.ConvertDuration);
        var result = await ConvertCoreAsync(inputPath, outputPath, options, ct).ConfigureAwait(false);
        _metrics.IncrementCounter(result.IsSuccess ? Constants.Metrics.ConvertSuccess : Constants.Metrics.ConvertFailure);
        if (!result.IsSuccess && result.Errors?.Count > 0)
            _metrics.RecordError(Constants.Metrics.ConvertDuration, new InvalidOperationException(result.Errors[0].Message));

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ConvertFileToStreamAsync(string inputPath, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath, nameof(inputPath));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        var format = options?.Format ?? _options.DefaultFormat;
        var ext = format.StartsWith(".", StringComparison.Ordinal) ? format : "." + format;
        var tempPath = FfmpegTempHelper.CreateTempFilePath(ext);
        try {
            var result = await ConvertFileToFileAsync(inputPath, tempPath, options, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
                return result;

            await FfmpegTempHelper.CopyTempFileToStreamAsync(tempPath, outputStream, ct).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        finally {
            FfmpegTempHelper.TryDelete(tempPath);
        }
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> ConvertFileToBytesAsync(string inputPath, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath, nameof(inputPath));
        var format = options?.Format ?? _options.DefaultFormat;
        var ext = format.StartsWith(".", StringComparison.Ordinal) ? format : "." + format;
        var tempPath = FfmpegTempHelper.CreateTempFilePath(ext);
        try {
            var result = await ConvertFileToFileAsync(inputPath, tempPath, options, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
                return Result<byte[]>.Failure(result.Errors ?? []);

            var bytes = await FfmpegTempHelper.ReadTempFileToBytesAsync(tempPath, ct).ConfigureAwait(false);
            return Result<byte[]>.Success(bytes);
        }
        finally {
            FfmpegTempHelper.TryDelete(tempPath);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ConvertStreamToStreamAsync(Stream inputStream, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        var inputPath = await FfmpegTempHelper.WriteStreamToTempFileAsync(inputStream, ".tmp", ct).ConfigureAwait(false);
        try {
            return await ConvertFileToStreamAsync(inputPath, outputStream, options, ct).ConfigureAwait(false);
        }
        finally {
            FfmpegTempHelper.TryDelete(inputPath);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ConvertStreamToFileAsync(Stream inputStream, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));
        var inputPath = await FfmpegTempHelper.WriteStreamToTempFileAsync(inputStream, ".tmp", ct).ConfigureAwait(false);
        try {
            return await ConvertFileToFileAsync(inputPath, outputPath, options, ct).ConfigureAwait(false);
        }
        finally {
            FfmpegTempHelper.TryDelete(inputPath);
        }
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> ConvertStreamToBytesAsync(Stream inputStream, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        var inputPath = await FfmpegTempHelper.WriteStreamToTempFileAsync(inputStream, ".tmp", ct).ConfigureAwait(false);
        try {
            return await ConvertFileToBytesAsync(inputPath, options, ct).ConfigureAwait(false);
        }
        finally {
            FfmpegTempHelper.TryDelete(inputPath);
        }
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> ConvertBytesToBytesAsync(byte[] inputBytes, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputBytes, nameof(inputBytes));
        var inputPath = await FfmpegTempHelper.WriteBytesToTempFileAsync(inputBytes, ".tmp", ct).ConfigureAwait(false);
        try {
            return await ConvertFileToBytesAsync(inputPath, options, ct).ConfigureAwait(false);
        }
        finally {
            FfmpegTempHelper.TryDelete(inputPath);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ConvertBytesToStreamAsync(byte[] inputBytes, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputBytes, nameof(inputBytes));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        var inputPath = await FfmpegTempHelper.WriteBytesToTempFileAsync(inputBytes, ".tmp", ct).ConfigureAwait(false);
        try {
            return await ConvertFileToStreamAsync(inputPath, outputStream, options, ct).ConfigureAwait(false);
        }
        finally {
            FfmpegTempHelper.TryDelete(inputPath);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ConvertBytesToFileAsync(byte[] inputBytes, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputBytes, nameof(inputBytes));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));
        var inputPath = await FfmpegTempHelper.WriteBytesToTempFileAsync(inputBytes, ".tmp", ct).ConfigureAwait(false);
        try {
            return await ConvertFileToFileAsync(inputPath, outputPath, options, ct).ConfigureAwait(false);
        }
        finally {
            FfmpegTempHelper.TryDelete(inputPath);
        }
    }

    private async Task<Result<bool>> ConvertCoreAsync(string inputPath, string outputPath, AudioConversionOptions? options, CancellationToken ct)
    {
        var opts = options ?? new AudioConversionOptions();
        var codec = opts.Codec ?? _options.DefaultCodec;
        var sampleRate = opts.SampleRate ?? _options.DefaultSampleRate;
        var channels = opts.Channels ?? _options.DefaultChannels;
        var format = opts.Format ?? _options.DefaultFormat;
        var builder = FFmpegCommandBuilder.New()
            .WithInput(inputPath)
            .WithOutput(outputPath)
            .WithCodec(codec)
            .WithSampleRate(sampleRate)
            .WithChannels(channels)
            .WithFormat(format)
            .WithDefaults(_options);

        builder = opts.Overwrite ? builder.Overwrite() : builder.NoOverwrite();
        builder = opts.NoVideo ? builder.NoVideo() : builder.WithVideo();
        var command = builder.Build();
        _logger.LogDebug("Converting audio: {Command}", command.GetFullCommand());
        var (exitCode, _, stderr) = await _runner.RunAsync(command.ExecutablePath, command.Arguments, ct).ConfigureAwait(false);
        if (exitCode != 0) {
            _logger.LogWarning("FFmpeg conversion failed with exit code {ExitCode}: {Stderr}", exitCode, stderr);
            return Result<bool>.Failure($"FFmpeg conversion failed: {stderr}", "FFmpegError");
        }

        return Result<bool>.Success(true);
    }

    private static AudioConversionOptions ToOptions(AudioConversionRequest request)
        => new() {
            Codec = request.Codec,
            SampleRate = request.SampleRate,
            Channels = request.Channels,
            Format = request.Format,
            Overwrite = request.Overwrite,
            NoVideo = request.NoVideo
        };
}