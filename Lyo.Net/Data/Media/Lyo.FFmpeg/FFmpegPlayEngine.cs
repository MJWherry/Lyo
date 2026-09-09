using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FFmpeg;

/// <summary>Shared ffplay runner. Per-call process state; concurrent play calls on one instance are safe.</summary>
internal sealed class FFmpegPlayEngine
{
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
    private readonly FFmpegOptions _options;
    private readonly FFmpegProcessRunner _runner;

    public FFmpegPlayEngine(FFmpegOptions options, ILogger logger, IMetrics? metrics)
    {
        _options = options;
        _logger = logger ?? NullLogger.Instance;
        _metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _runner = new(options, _logger);
    }

    public async Task<Result<bool>> PlayAsync(string filePath, bool noDisplay, bool autoExit, MediaIoMode ioMode, TimeSpan? startTime, TimeSpan? duration, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filePath);
        FFmpegPathKind.ThrowIfLocalFileMissing(filePath);
        using var timer = _metrics.StartTimer(Constants.Metrics.PlayDuration);
        var result = await PlayCoreAsync(filePath, stdin: null, noDisplay, autoExit, startTime, duration, ct).ConfigureAwait(false);
        Track(result);
        return result;
    }

    public async Task<Result<bool>> PlayStreamAsync(Stream stream, bool noDisplay, bool autoExit, MediaIoMode ioMode, TimeSpan? startTime, TimeSpan? duration, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        if (ioMode == MediaIoMode.Pipe)
            return await PlayCoreAsync(FFmpegPathKind.PipeInput, stream, noDisplay, autoExit, startTime, duration, ct).ConfigureAwait(false);

        var inputPath = await FFmpegTempHelper.WriteStreamToTempFileAsync(stream, ".tmp", ct).ConfigureAwait(false);
        try {
            return await PlayAsync(inputPath, noDisplay, autoExit, ioMode, startTime, duration, ct).ConfigureAwait(false);
        }
        finally {
            FFmpegTempHelper.TryDelete(inputPath);
        }
    }

    internal static List<string> BuildPlayArguments(string input, bool noDisplay, bool autoExit, TimeSpan? startTime, TimeSpan? duration, bool suppressOutput)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(input);
        var args = new List<string>();
        if (suppressOutput)
            args.AddRange(["-loglevel", "quiet"]);

        if (autoExit)
            args.Add("-autoexit");

        if (noDisplay)
            args.Add("-nodisp");

        if (startTime is { } ss) {
            args.Add("-ss");
            args.Add(FFmpegCommandBuilder.FormatTimestamp(ss));
        }

        args.Add("-i");
        args.Add(input);
        if (duration is { } dur) {
            args.Add("-t");
            args.Add(FFmpegCommandBuilder.FormatTimestamp(dur));
        }

        return args;
    }

    private async Task<Result<bool>> PlayCoreAsync(string input, Stream? stdin, bool noDisplay, bool autoExit, TimeSpan? startTime, TimeSpan? duration, CancellationToken ct)
    {
        var args = BuildPlayArguments(input, noDisplay, autoExit, startTime, duration, _options.SuppressFfplayOutput);
        _logger.LogDebug("Playing: {Ffplay} {Args}", _runner.GetFfplayPath(), string.Join(" ", args));
        try {
            var run = await _runner.RunAsync(
                    new() {
                        ExecutablePath = _runner.GetFfplayPath(),
                        Arguments = args,
                        StandardInput = stdin,
                        Timeout = _options.ProcessTimeout,
                        CancellationToken = ct
                    })
                .ConfigureAwait(false);
            if (run.ExitCode == 0)
                return Result<bool>.Success(true);

            return Result<bool>.Failure($"Ffplay failed with exit code {run.ExitCode}: {run.StdErr}", Constants.Errors.FfplayError);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Error playing {Input}", input);
            return Result<bool>.Failure(ex, Constants.Errors.FfplayError);
        }
    }

    private void Track(Result<bool> result)
    {
        _metrics.IncrementCounter(result.IsSuccess ? Constants.Metrics.PlaySuccess : Constants.Metrics.PlayFailure);
        if (!result.IsSuccess && result.Errors?.Count > 0)
            _metrics.RecordError(Constants.Metrics.PlayDuration, new InvalidOperationException(result.Errors[0].Message));
    }
}
