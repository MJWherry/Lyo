using Lyo.Common.Core.Records;
using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FFmpeg;

/// <summary><see cref="IAudioConverter" /> backed by ffmpeg. Concurrent convert calls on one instance are safe (one process per call).</summary>
public sealed class FFmpegAudioConverter : IAudioConverter
{
    private readonly FFmpegConvertEngine _engine;
    private readonly FFmpegOptions _options;

    /// <summary>Creates a converter. Metrics are recorded only when <see cref="FFmpegOptions.EnableMetrics" /> is on and <paramref name="metrics" /> is provided.</summary>
    public FFmpegAudioConverter(FFmpegOptions? options = null, ILogger<FFmpegAudioConverter>? logger = null, IMetrics? metrics = null)
    {
        _options = options ?? new FFmpegOptions();
        _engine = new(_options, logger ?? NullLogger<FFmpegAudioConverter>.Instance, metrics);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ConvertAsync(AudioConversionRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        request.Validate();
        return await ConvertFileToFileAsync(request.InputPath, request.OutputPath, request.Options, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<bool>> ConvertFileToFileAsync(string inputPath, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertFileToFileAsync(inputPath, outputPath, Prepare(options, fileToFile: true), ct);

    /// <inheritdoc />
    public Task<Result<bool>> ConvertFileToStreamAsync(string inputPath, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertFileToStreamAsync(inputPath, outputStream, Prepare(options, fileToFile: false), ct);

    /// <inheritdoc />
    public Task<Result<byte[]>> ConvertFileToBytesAsync(string inputPath, AudioConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertFileToBytesAsync(inputPath, Prepare(options, fileToFile: false), ct);

    /// <inheritdoc />
    public Task<Result<bool>> ConvertStreamToStreamAsync(Stream inputStream, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertStreamToStreamAsync(inputStream, outputStream, Prepare(options, fileToFile: false), ct);

    /// <inheritdoc />
    public Task<Result<bool>> ConvertStreamToFileAsync(Stream inputStream, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertStreamToFileAsync(inputStream, outputPath, Prepare(options, fileToFile: false), ct);

    /// <inheritdoc />
    public Task<Result<byte[]>> ConvertStreamToBytesAsync(Stream inputStream, AudioConversionOptions? options = null, CancellationToken ct = default)
        => _engine.ConvertStreamToBytesAsync(inputStream, Prepare(options, fileToFile: false), ct);

    /// <inheritdoc />
    public Task<Result<byte[]>> ConvertBytesToBytesAsync(byte[] inputBytes, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputBytes);
        return ConvertStreamToBytesAsync(new MemoryStream(inputBytes, writable: false), options, ct);
    }

    /// <inheritdoc />
    public Task<Result<bool>> ConvertBytesToStreamAsync(byte[] inputBytes, Stream outputStream, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputBytes);
        return ConvertStreamToStreamAsync(new MemoryStream(inputBytes, writable: false), outputStream, options, ct);
    }

    /// <inheritdoc />
    public Task<Result<bool>> ConvertBytesToFileAsync(byte[] inputBytes, string outputPath, AudioConversionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(inputBytes);
        return ConvertStreamToFileAsync(new MemoryStream(inputBytes, writable: false), outputPath, options, ct);
    }

    /// <inheritdoc />
    public Task<Result<IMediaProcessSession>> StartConvertAsync(string inputPathOrUrl, AudioConversionOptions options, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        return _engine.StartConvertAsync(inputPathOrUrl, ConvertKnobs.FromAudio(options), ct);
    }

    /// <inheritdoc />
    public Task<Result<IMediaProcessSession>> StartConvertAsync(Stream input, AudioConversionOptions options, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        return _engine.StartConvertAsync(input, ConvertKnobs.FromAudio(options), ct);
    }

    /// <inheritdoc />
    public Task<Result<IMediaProcessSession>> StartRawPcmAsync(string inputPathOrUrl, int sampleRate = 48000, int channels = 2, CancellationToken ct = default)
        => StartConvertAsync(inputPathOrUrl, AudioConversionOptions.ForRawPcm(sampleRate, channels), ct);

    private ConvertKnobs Prepare(AudioConversionOptions? options, bool fileToFile)
    {
        var opts = options ?? DefaultAudio();
        opts.Validate();
        var knobs = ConvertKnobs.FromAudio(opts);
        FFmpegConvertEngine.ThrowIfFileToFilePipe(knobs, fileToFile);
        return knobs;
    }

    private AudioConversionOptions DefaultAudio()
        => AudioConversionOptions.ForAudio(
            _options.DefaultSampleRate,
            _options.DefaultChannels,
            AudioEncoder.TryFromId(FFmpegCodecMap.FromFfmpegAudio(_options.DefaultCodec)) ?? AudioEncoder.PcmS16le,
            MediaContainer.TryFromFormat(_options.DefaultFormat) ?? MediaContainer.Wav) with {
            Overwrite = _options.DefaultOverwrite
        };
}
