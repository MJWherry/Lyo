using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Lyo.Exceptions;
using Lyo.Ffmpeg.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Ffmpeg;

/// <summary>Runs FFmpeg/FFprobe processes using CliWrap.</summary>
internal sealed class FfmpegProcessRunner(FfmpegOptions? options, ILogger? logger)
{
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private readonly FfmpegOptions _options = options ?? new FfmpegOptions();

    public async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(string executablePath, string arguments, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(executablePath, nameof(executablePath));
        ArgumentHelpers.ThrowIfNull(arguments, nameof(arguments));
        var cmd = Cli.Wrap(executablePath).WithArguments(arguments);
        if (_options.ProcessOutputMode == FfmpegProcessOutputMode.Passthrough) {
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            cmd = cmd.WithStandardOutputPipe(
                    PipeTarget.ToDelegate(line => {
                        stdout.AppendLine(line);
                        _logger.LogDebug("{StdOut}", line);
                    }))
                .WithStandardErrorPipe(
                    PipeTarget.ToDelegate(line => {
                        stderr.AppendLine(line);
                        _logger.LogDebug("{StdErr}", line);
                    }));

            var result = await cmd.ExecuteAsync(ct).ConfigureAwait(false);
            return (result.ExitCode, stdout.ToString(), stderr.ToString());
        }

        var buffered = await cmd.ExecuteBufferedAsync(ct).ConfigureAwait(false);
        return (buffered.ExitCode, buffered.StandardOutput, buffered.StandardError);
    }

    public string GetFfmpegPath() => _options.FfmpegPath ?? "ffmpeg";

    public string GetFfprobePath() => _options.FfprobePath ?? "ffprobe";
}