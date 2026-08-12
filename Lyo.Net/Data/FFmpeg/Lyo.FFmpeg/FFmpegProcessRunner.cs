using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FFmpeg;

/// <summary>Runs FFmpeg/FFprobe processes using CliWrap.</summary>
internal sealed class FFmpegProcessRunner(FFmpegOptions? options, ILogger? logger)
{
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private readonly FFmpegOptions _options = options ?? new FFmpegOptions();

    public async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(string executablePath, string arguments, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentHelpers.ThrowIfNull(arguments);
        var cmd = Cli.Wrap(executablePath).WithArguments(arguments);
        if (_options.ProcessOutputMode == FFmpegProcessOutputMode.Passthrough) {
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

    public string GetFFmpegPath() => _options.FFmpegPath ?? "ffmpeg";

    public string GetFfprobePath() => _options.FfprobePath ?? "ffprobe";
}