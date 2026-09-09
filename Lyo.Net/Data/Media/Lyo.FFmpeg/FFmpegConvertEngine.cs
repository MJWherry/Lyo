using System.IO.Pipelines;
using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FFmpeg;

/// <summary>Shared ffmpeg convert runner used by audio and video facades. Per-call process state; two ExecuteAsync calls may run at once.</summary>
internal sealed class FFmpegConvertEngine
{
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
    private readonly FFmpegOptions _options;
    private readonly FFmpegProcessRunner _runner;

    public FFmpegConvertEngine(FFmpegOptions options, ILogger logger, IMetrics? metrics)
    {
        _options = options;
        _logger = logger ?? NullLogger.Instance;
        _metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _runner = new(options, _logger);
    }

    public async Task<Result<bool>> ConvertFileToFileAsync(string inputPath, string outputPath, ConvertKnobs opts, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        FFmpegPathKind.ThrowIfLocalFileMissing(inputPath);
        using var timer = _metrics.StartTimer(Constants.Metrics.ConvertDuration);
        var result = await RunToCompletionAsync(inputPath, outputPath, stdin: null, stdout: null, opts, applyTimeout: true, ct).ConfigureAwait(false);
        TrackConvert(result);
        return result;
    }

    public async Task<Result<bool>> ConvertFileToStreamAsync(string inputPath, Stream outputStream, ConvertKnobs opts, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        FFmpegPathKind.ThrowIfLocalFileMissing(inputPath);
        if (opts.IoMode == MediaIoMode.Pipe)
            return await RunToCompletionAsync(inputPath, FFmpegPathKind.PipeOutput, stdin: null, stdout: outputStream, opts, applyTimeout: true, ct).ConfigureAwait(false);

        var tempPath = FFmpegTempHelper.CreateTempFilePath(ExtensionFor(opts));
        try {
            var result = await ConvertFileToFileAsync(inputPath, tempPath, opts, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
                return result;

            await FFmpegTempHelper.CopyTempFileToStreamAsync(tempPath, outputStream, ct).ConfigureAwait(false);
            return Result<bool>.Success(true);
        }
        finally {
            FFmpegTempHelper.TryDelete(tempPath);
        }
    }

    public async Task<Result<byte[]>> ConvertFileToBytesAsync(string inputPath, ConvertKnobs opts, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var result = await ConvertFileToStreamAsync(inputPath, buffer, opts, ct).ConfigureAwait(false);
        return result.IsSuccess ? Result<byte[]>.Success(buffer.ToArray()) : Result<byte[]>.Failure(result.Errors ?? []);
    }

    public async Task<Result<bool>> ConvertStreamToStreamAsync(Stream inputStream, Stream outputStream, ConvertKnobs opts, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        if (opts.IoMode == MediaIoMode.Pipe)
            return await RunToCompletionAsync(FFmpegPathKind.PipeInput, FFmpegPathKind.PipeOutput, inputStream, outputStream, opts, applyTimeout: true, ct)
                .ConfigureAwait(false);

        var inputPath = await FFmpegTempHelper.WriteStreamToTempFileAsync(inputStream, ".tmp", ct).ConfigureAwait(false);
        try {
            return await ConvertFileToStreamAsync(inputPath, outputStream, opts, ct).ConfigureAwait(false);
        }
        finally {
            FFmpegTempHelper.TryDelete(inputPath);
        }
    }

    public async Task<Result<bool>> ConvertStreamToFileAsync(Stream inputStream, string outputPath, ConvertKnobs opts, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(inputStream);
        OperationHelpers.ThrowIfNotReadable(inputStream, $"Stream '{nameof(inputStream)}' must be readable.");
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        if (opts.IoMode == MediaIoMode.Pipe)
            return await RunToCompletionAsync(FFmpegPathKind.PipeInput, outputPath, inputStream, stdout: null, opts, applyTimeout: true, ct).ConfigureAwait(false);

        var inputPath = await FFmpegTempHelper.WriteStreamToTempFileAsync(inputStream, ".tmp", ct).ConfigureAwait(false);
        try {
            return await ConvertFileToFileAsync(inputPath, outputPath, opts, ct).ConfigureAwait(false);
        }
        finally {
            FFmpegTempHelper.TryDelete(inputPath);
        }
    }

    public async Task<Result<byte[]>> ConvertStreamToBytesAsync(Stream inputStream, ConvertKnobs opts, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var result = await ConvertStreamToStreamAsync(inputStream, buffer, opts, ct).ConfigureAwait(false);
        return result.IsSuccess ? Result<byte[]>.Success(buffer.ToArray()) : Result<byte[]>.Failure(result.Errors ?? []);
    }

    public Task<Result<IMediaProcessSession>> StartConvertAsync(string inputPathOrUrl, ConvertKnobs opts, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPathOrUrl);
        FFmpegPathKind.ThrowIfLocalFileMissing(inputPathOrUrl);
        return StartCoreAsync(inputPathOrUrl, stdin: null, opts, ct);
    }

    public Task<Result<IMediaProcessSession>> StartConvertAsync(Stream input, ConvertKnobs opts, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(input);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        return StartCoreAsync(FFmpegPathKind.PipeInput, input, opts, ct);
    }

    public async Task<Result<bool>> ExtractFrameAsync(string inputPath, string outputPath, TimeSpan? at, ConvertKnobs opts, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        FFmpegPathKind.ThrowIfLocalFileMissing(inputPath);
        var seek = at ?? TimeSpan.Zero;
        var builder = FFmpegCommandBuilder.New()
            .WithDefaults(_options)
            .WithInput(inputPath)
            .WithOutput(outputPath)
            .WithStartTime(seek)
            .WithArgument("-frames:v")
            .WithArgument("1");
        if (opts.Overwrite)
            builder.Overwrite();
        else
            builder.NoOverwrite();

        if (opts.Format != null)
            builder.WithFormat(opts.Format);

        var command = builder.Build();
        using var timer = _metrics.StartTimer(Constants.Metrics.ExtractDuration);
        var run = await _runner.RunAsync(
                new() {
                    ExecutablePath = command.ExecutablePath,
                    Arguments = command.ArgumentList,
                    Timeout = _options.ProcessTimeout,
                    CancellationToken = ct
                })
            .ConfigureAwait(false);
        var result = ToResult(run, Constants.Errors.FFmpegError);
        _metrics.IncrementCounter(result.IsSuccess ? Constants.Metrics.ExtractSuccess : Constants.Metrics.ExtractFailure);
        if (!result.IsSuccess && result.Errors?.Count > 0)
            _metrics.RecordError(Constants.Metrics.ExtractDuration, new InvalidOperationException(result.Errors[0].Message));

        return result;
    }

    public static void ThrowIfFileToFilePipe(ConvertKnobs opts, bool fileToFile)
        => OperationHelpers.ThrowIf(fileToFile && opts.IoMode == MediaIoMode.Pipe, "IoMode.Pipe requires a stream endpoint. File-to-file convert cannot use pipes.");

    private async Task<Result<IMediaProcessSession>> StartCoreAsync(string input, Stream? stdin, ConvertKnobs options, CancellationToken ct)
    {
        var opts = options with { IoMode = MediaIoMode.Pipe };
        var pipe = new Pipe();
        var writerStream = pipe.Writer.AsStream();
        var readerStream = pipe.Reader.AsStream();
        var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var command = BuildCommand(input, FFmpegPathKind.PipeOutput, opts, outputIsPipe: true);
        var runTask = _runner.RunAsync(
            new() {
                ExecutablePath = command.ExecutablePath,
                Arguments = command.ArgumentList,
                StandardInput = stdin,
                StandardOutput = writerStream,
                Progress = opts.Progress,
                KnownDuration = opts.KnownDuration,
                ProgressOnStdout = false,
                Timeout = null,
                CancellationToken = sessionCts.Token
            });
        var completion = AwaitSessionAsync(runTask, pipe.Writer);
        IMediaProcessSession session = new MediaProcessSession(readerStream, completion, pipe.Writer, sessionCts);
        return Result<IMediaProcessSession>.Success(session);
    }

    private static async Task<Result<bool>> AwaitSessionAsync(Task<FFmpegProcessResult> run, PipeWriter writer)
    {
        try {
            var result = await run.ConfigureAwait(false);
            await writer.CompleteAsync().ConfigureAwait(false);
            return ToResult(result, Constants.Errors.FFmpegError);
        }
        catch (Exception ex) {
            try {
                await writer.CompleteAsync(ex).ConfigureAwait(false);
            }
            catch {
            }

            return Result<bool>.Failure(ex, Constants.Errors.FFmpegError);
        }
    }

    private async Task<Result<bool>> RunToCompletionAsync(
        string input,
        string output,
        Stream? stdin,
        Stream? stdout,
        ConvertKnobs opts,
        bool applyTimeout,
        CancellationToken ct)
    {
        var outputIsPipe = stdout != null || string.Equals(output, FFmpegPathKind.PipeOutput, StringComparison.Ordinal);
        var command = BuildCommand(input, output, opts, outputIsPipe);
        _logger.LogDebug("Converting: {Command}", command.GetFullCommand());
        var run = await _runner.RunAsync(
                new() {
                    ExecutablePath = command.ExecutablePath,
                    Arguments = command.ArgumentList,
                    StandardInput = stdin,
                    StandardOutput = stdout,
                    Progress = opts.Progress,
                    KnownDuration = opts.KnownDuration,
                    ProgressOnStdout = opts.Progress != null && !outputIsPipe,
                    Timeout = applyTimeout ? _options.ProcessTimeout : null,
                    CancellationToken = ct
                })
            .ConfigureAwait(false);
        return ToResult(run, Constants.Errors.FFmpegError);
    }

    private FFmpegCommand BuildCommand(string input, string output, ConvertKnobs opts, bool outputIsPipe)
    {
        ArgumentHelpers.ThrowIf(
            opts.FragmentedOutput && opts.Format == null && !output.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(output, FFmpegPathKind.PipeOutput, StringComparison.Ordinal),
            "FragmentedOutput requires Format Mp4 or a .mp4 output path.");
        ArgumentHelpers.ThrowIf(
            opts.FragmentedOutput && opts.Format == null && string.Equals(output, FFmpegPathKind.PipeOutput, StringComparison.Ordinal),
            "FragmentedOutput requires Format Mp4 when outputting to a pipe.");

        var builder = FFmpegCommandBuilder.New().WithDefaults(_options).WithInput(input).WithOutput(output).ApplyKnobs(opts);
        if (opts.Progress == null)
            return builder.Build();

        builder.WithArgument("-progress").WithArgument(outputIsPipe ? "pipe:2" : "pipe:1");
        return builder.Build();
    }

    private static string ExtensionFor(ConvertKnobs opts) => opts.Format?.Extension ?? ".tmp";

    private void TrackConvert(Result<bool> result)
    {
        _metrics.IncrementCounter(result.IsSuccess ? Constants.Metrics.ConvertSuccess : Constants.Metrics.ConvertFailure);
        if (!result.IsSuccess && result.Errors?.Count > 0)
            _metrics.RecordError(Constants.Metrics.ConvertDuration, new InvalidOperationException(result.Errors[0].Message));
    }

    private static Result<bool> ToResult(FFmpegProcessResult run, string errorCode)
    {
        if (run.ExitCode == 0)
            return Result<bool>.Success(true);

        var detail = string.IsNullOrWhiteSpace(run.StdErr) ? run.StdOut : run.StdErr;
        return Result<bool>.Failure($"FFmpeg failed with exit code {run.ExitCode}: {detail}", errorCode);
    }
}
