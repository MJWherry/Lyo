using System.Text;
using CliWrap;
using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FFmpeg;

/// <summary>Launch parameters for one ffmpeg/ffprobe/ffplay process.</summary>
internal sealed record FFmpegProcessSpec
{
    public required string ExecutablePath { get; init; }

    public required IReadOnlyList<string> Arguments { get; init; }

    public Stream? StandardInput { get; init; }

    public Stream? StandardOutput { get; init; }

    public IProgress<MediaProgress>? Progress { get; init; }

    public TimeSpan? KnownDuration { get; init; }

    /// <summary>When true, <c>-progress</c> is already on stdout (file output). When false and Progress is set, parse stderr.</summary>
    public bool ProgressOnStdout { get; init; }

    public TimeSpan? Timeout { get; init; }

    public CancellationToken CancellationToken { get; init; }
}

/// <summary>Exit snapshot from <see cref="FFmpegProcessRunner" />.</summary>
internal readonly struct FFmpegProcessResult(int exitCode, string stdOut, string stdErr)
{
    public int ExitCode { get; } = exitCode;

    public string StdOut { get; } = stdOut;

    public string StdErr { get; } = stdErr;
}

/// <summary>Launches ffmpeg, ffprobe, and ffplay through CliWrap with ArgumentList and streaming pipes.</summary>
internal sealed class FFmpegProcessRunner(FFmpegOptions? options, ILogger? logger)
{
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private readonly FFmpegOptions _options = options ?? new FFmpegOptions();

    public string GetFFmpegPath() => string.IsNullOrWhiteSpace(_options.FFmpegPath) ? "ffmpeg" : _options.FFmpegPath;

    public string GetFfprobePath() => string.IsNullOrWhiteSpace(_options.FfprobePath) ? "ffprobe" : _options.FfprobePath;

    public string GetFfplayPath() => FFmpegPathKind.ResolveFfplayPath(_options);

    /// <summary>Runs the process to completion.</summary>
    public async Task<FFmpegProcessResult> RunAsync(FFmpegProcessSpec spec)
    {
        ArgumentHelpers.ThrowIfNull(spec);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(spec.ExecutablePath);
        ArgumentHelpers.ThrowIfNull(spec.Arguments);
        using var cts = CreateTimeoutCts(spec.CancellationToken, spec.Timeout);
        var stderr = new StringBuilder();
        var stdoutCaptured = new StringBuilder();
        var progressState = new FFmpegProgressParser(spec.Progress, spec.KnownDuration);
        var cmd = BuildCommand(spec, stderr, stdoutCaptured, progressState, captureStdout: spec.StandardOutput == null);
        var result = await cmd.ExecuteAsync(cts.Token).ConfigureAwait(false);
        return new(result.ExitCode, stdoutCaptured.ToString(), stderr.ToString());
    }

    /// <summary>Starts the process without waiting. Caller reads <paramref name="sessionStdoutWriter" />. Completes the pipe writer when ffmpeg exits.</summary>
    public Task<FFmpegProcessResult> StartAsync(FFmpegProcessSpec spec, Stream sessionStdoutWriter)
    {
        ArgumentHelpers.ThrowIfNull(spec);
        ArgumentHelpers.ThrowIfNull(sessionStdoutWriter);
        var linked = spec with { StandardOutput = sessionStdoutWriter, Timeout = null };
        return RunAsync(linked);
    }

    private Command BuildCommand(
        FFmpegProcessSpec spec,
        StringBuilder stderr,
        StringBuilder stdoutCaptured,
        FFmpegProgressParser progressState,
        bool captureStdout)
    {
        var cmd = Cli.Wrap(spec.ExecutablePath)
            .WithArguments(spec.Arguments)
            .WithValidation(CommandResultValidation.None);
        if (spec.StandardInput != null)
            cmd = cmd.WithStandardInputPipe(PipeSource.FromStream(spec.StandardInput));

        if (spec.StandardOutput != null)
            cmd = cmd.WithStandardOutputPipe(PipeTarget.ToStream(spec.StandardOutput));
        else if (spec.ProgressOnStdout && spec.Progress != null)
            cmd = cmd.WithStandardOutputPipe(PipeTarget.ToDelegate(line => {
                progressState.IngestLine(line);
                if (_options.ProcessOutputMode == FFmpegProcessOutputMode.Passthrough)
                    _logger.LogDebug("{StdOut}", line);
            }));
        else
            cmd = cmd.WithStandardOutputPipe(
                PipeTarget.ToDelegate(line => {
                    stdoutCaptured.AppendLine(line);
                    if (_options.ProcessOutputMode == FFmpegProcessOutputMode.Passthrough)
                        _logger.LogDebug("{StdOut}", line);
                }));

        cmd = cmd.WithStandardErrorPipe(
            PipeTarget.ToDelegate(line => {
                stderr.AppendLine(line);
                if (!spec.ProgressOnStdout)
                    progressState.IngestLine(line);

                if (_options.ProcessOutputMode == FFmpegProcessOutputMode.Passthrough)
                    _logger.LogDebug("{StdErr}", line);
            }));

        return cmd;
    }

    private static CancellationTokenSource CreateTimeoutCts(CancellationToken caller, TimeSpan? timeout)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(caller);
        if (timeout is { } t && t > TimeSpan.Zero)
            cts.CancelAfter(t);

        return cts;
    }
}
