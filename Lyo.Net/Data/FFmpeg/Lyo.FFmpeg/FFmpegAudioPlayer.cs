using System.Runtime.InteropServices;
using CliWrap;
using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FFmpeg;

/// <summary>FFmpeg-based implementation of IAudioPlayer using ffplay.</summary>
public sealed class FFmpegAudioPlayer : IAudioPlayer
{
    private readonly ILogger<FFmpegAudioPlayer> _logger;
    private readonly IMetrics _metrics;
    private readonly FFmpegOptions _options;

    public FFmpegAudioPlayer(FFmpegOptions? options = null, ILogger<FFmpegAudioPlayer>? logger = null, IMetrics? metrics = null)
    {
        _options = options ?? new FFmpegOptions();
        _logger = logger ?? NullLogger<FFmpegAudioPlayer>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> PlayAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(filePath);
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
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        var inputPath = await FFmpegTempHelper.WriteStreamToTempFileAsync(stream, ".tmp", ct).ConfigureAwait(false);
        try {
            return await PlayAsync(inputPath, ct).ConfigureAwait(false);
        }
        finally {
            FFmpegTempHelper.TryDelete(inputPath);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> PlayBytesAsync(byte[] bytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        var inputPath = await FFmpegTempHelper.WriteBytesToTempFileAsync(bytes, ".tmp", ct).ConfigureAwait(false);
        try {
            return await PlayAsync(inputPath, ct).ConfigureAwait(false);
        }
        finally {
            FFmpegTempHelper.TryDelete(inputPath);
        }
    }

    private async Task<Result<bool>> PlayCoreAsync(string filePath, CancellationToken ct)
    {
        var ffplayPath = _options.FfplayPath ?? (_options.FFmpegPath != null
            ? Path.Combine(Path.GetDirectoryName(_options.FFmpegPath) ?? "", "ffplay" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""))
            : "ffplay");

        var args = _options.SuppressFfplayOutput
            ? $"-loglevel quiet -autoexit -nodisp \"{filePath.Replace("\"", "\\\"")}\""
            : $"-autoexit -nodisp \"{filePath.Replace("\"", "\\\"")}\"";

        _logger.LogDebug("Playing: {FfplayPath} {Args}", ffplayPath, args);
        try {
            var cmd = Cli.Wrap(ffplayPath).WithArguments(args);
            if (_options.SuppressFfplayOutput)
                cmd = cmd.WithStandardOutputPipe(PipeTarget.Null).WithStandardErrorPipe(PipeTarget.Null);

            _ = await cmd.ExecuteAsync(ct).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Error playing audio file {FilePath}", filePath);
            return Result<bool>.Failure(ex, "FfplayError");
        }
    }
}