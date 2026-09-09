using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FFmpeg;

/// <summary>Shared ffprobe runner. Per-call process state; concurrent probes on one instance are safe.</summary>
internal sealed class FFmpegProbeEngine
{
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
    private readonly FFmpegOptions _options;
    private readonly FFmpegProcessRunner _runner;

    public FFmpegProbeEngine(FFmpegOptions options, ILogger logger, IMetrics? metrics)
    {
        _options = options;
        _logger = logger ?? NullLogger.Instance;
        _metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _runner = new(options, _logger);
    }

    public async Task<Result<MediaProbeResult>> ProbeAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath);
        FFmpegPathKind.ThrowIfLocalFileMissing(filePath);
        using var timer = _metrics.StartTimer(Constants.Metrics.ProbeDuration);
        var result = await ProbeCoreAsync(filePath, displayPath: filePath, ct).ConfigureAwait(false);
        _metrics.IncrementCounter(result.IsSuccess ? Constants.Metrics.ProbeSuccess : Constants.Metrics.ProbeFailure);
        if (!result.IsSuccess && result.Errors?.Count > 0)
            _metrics.RecordError(Constants.Metrics.ProbeDuration, new InvalidOperationException(result.Errors[0].Message));

        return result;
    }

    public async Task<Result<MediaProbeResult>> ProbeStreamAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        var inputPath = await FFmpegTempHelper.WriteStreamToTempFileAsync(stream, ".tmp", ct).ConfigureAwait(false);
        try {
            using var timer = _metrics.StartTimer(Constants.Metrics.ProbeDuration);
            var result = await ProbeCoreAsync(inputPath, displayPath: null, ct).ConfigureAwait(false);
            _metrics.IncrementCounter(result.IsSuccess ? Constants.Metrics.ProbeSuccess : Constants.Metrics.ProbeFailure);
            return result;
        }
        finally {
            FFmpegTempHelper.TryDelete(inputPath);
        }
    }

    public async Task<Result<MediaProbeResult>> ProbeBytesAsync(byte[] bytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        var inputPath = await FFmpegTempHelper.WriteBytesToTempFileAsync(bytes, ".tmp", ct).ConfigureAwait(false);
        try {
            using var timer = _metrics.StartTimer(Constants.Metrics.ProbeDuration);
            var result = await ProbeCoreAsync(inputPath, displayPath: null, ct).ConfigureAwait(false);
            _metrics.IncrementCounter(result.IsSuccess ? Constants.Metrics.ProbeSuccess : Constants.Metrics.ProbeFailure);
            return result;
        }
        finally {
            FFmpegTempHelper.TryDelete(inputPath);
        }
    }

    private async Task<Result<MediaProbeResult>> ProbeCoreAsync(string filePath, string? displayPath, CancellationToken ct)
    {
        var ffprobePath = _runner.GetFfprobePath();
        var args = new[] { "-v", "quiet", "-of", "json", "-show_format", "-show_streams", filePath };
        _logger.LogDebug("Probing: {FfprobePath} {Args}", ffprobePath, string.Join(" ", args));
        var run = await _runner.RunAsync(
                new() {
                    ExecutablePath = ffprobePath,
                    Arguments = args,
                    Timeout = _options.ProcessTimeout,
                    CancellationToken = ct
                })
            .ConfigureAwait(false);
        if (run.ExitCode != 0)
            return Result<MediaProbeResult>.Failure($"FFprobe failed: {run.StdErr}", Constants.Errors.FFprobeError);

        return FfprobeJsonParser.Parse(displayPath, run.StdOut);
    }
}
