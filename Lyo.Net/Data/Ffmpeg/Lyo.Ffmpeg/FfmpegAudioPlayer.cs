using System.Runtime.InteropServices;
using CliWrap;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Ffmpeg.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Ffmpeg;

/// <summary>FFmpeg-based implementation of IAudioPlayer using ffplay.</summary>
public sealed class FfmpegAudioPlayer : IAudioPlayer
{
    private readonly ILogger<FfmpegAudioPlayer> _logger;
    private readonly IMetrics _metrics;
    private readonly FfmpegOptions _options;

    public FfmpegAudioPlayer(FfmpegOptions? options = null, ILogger<FfmpegAudioPlayer>? logger = null, IMetrics? metrics = null)
    {
        _options = options ?? new FfmpegOptions();
        _logger = logger ?? NullLogger<FfmpegAudioPlayer>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> PlayAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(filePath, nameof(filePath));
        using var timer = _metrics.StartTimer(Constants.Metrics.PlayDuration);
        var result = await PlayCoreAsync(filePath, ct).ConfigureAwait(false);
        _metrics.IncrementCounter(result.IsSuccess ? Constants.Metrics.PlaySuccess : Constants.Metrics.PlayFailure);
        if (!result.IsSuccess && result.Errors?.Count > 0)
            _metrics.RecordError(Constants.Metrics.PlayDuration, new InvalidOperationException(result.Errors[0].Message));

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> PlayStreamAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream, nameof(stream));
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        var inputPath = await FfmpegTempHelper.WriteStreamToTempFileAsync(stream, ".tmp", ct).ConfigureAwait(false);
        try {
            return await PlayAsync(inputPath, ct).ConfigureAwait(false);
        }
        finally {
            FfmpegTempHelper.TryDelete(inputPath);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> PlayBytesAsync(byte[] bytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(bytes, nameof(bytes));
        var inputPath = await FfmpegTempHelper.WriteBytesToTempFileAsync(bytes, ".tmp", ct).ConfigureAwait(false);
        try {
            return await PlayAsync(inputPath, ct).ConfigureAwait(false);
        }
        finally {
            FfmpegTempHelper.TryDelete(inputPath);
        }
    }

    private async Task<Result<bool>> PlayCoreAsync(string filePath, CancellationToken ct)
    {
        var ffplayPath = _options.FfplayPath ?? (_options.FfmpegPath != null
            ? Path.Combine(Path.GetDirectoryName(_options.FfmpegPath) ?? "", "ffplay" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""))
            : "ffplay");

        var args = _options.SuppressFfplayOutput
            ? $"-loglevel quiet -autoexit -nodisp \"{filePath.Replace("\"", "\\\"")}\""
            : $"-autoexit -nodisp \"{filePath.Replace("\"", "\\\"")}\"";

        _logger.LogDebug("Playing: {FfplayPath} {Args}", ffplayPath, args);
        try {
            var cmd = Cli.Wrap(ffplayPath).WithArguments(args);
            if (_options.SuppressFfplayOutput)
                cmd = cmd.WithStandardOutputPipe(PipeTarget.Null).WithStandardErrorPipe(PipeTarget.Null);

            var result = await cmd.ExecuteAsync(ct).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Error playing audio file {FilePath}", filePath);
            return Result<bool>.Failure(ex, "FfplayError");
        }
    }
}