using System.Text.Json;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Ffmpeg.Models;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Ffmpeg;

/// <summary>FFmpeg-based implementation of IAudioProber using ffprobe.</summary>
public sealed class FfmpegAudioProber : IAudioProber
{
    private readonly ILogger<FfmpegAudioProber> _logger;
    private readonly IMetrics _metrics;
    private readonly FfmpegOptions _options;
    private readonly FfmpegProcessRunner _runner;

    public FfmpegAudioProber(FfmpegOptions? options = null, ILogger<FfmpegAudioProber>? logger = null, IMetrics? metrics = null)
    {
        _options = options ?? new FfmpegOptions();
        _logger = logger ?? NullLogger<FfmpegAudioProber>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _runner = new(_options, _logger);
    }

    /// <inheritdoc />
    public async Task<Result<AudioProbeResult>> ProbeAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(filePath, nameof(filePath));
        using var timer = _metrics.StartTimer(Constants.Metrics.ProbeDuration);
        var result = await ProbeCoreAsync(filePath, ct).ConfigureAwait(false);
        _metrics.IncrementCounter(result.IsSuccess ? Constants.Metrics.ProbeSuccess : Constants.Metrics.ProbeFailure);
        if (!result.IsSuccess && result.Errors?.Count > 0)
            _metrics.RecordError(Constants.Metrics.ProbeDuration, new InvalidOperationException(result.Errors[0].Message));

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<AudioProbeResult>> ProbeStreamAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(stream, nameof(stream));
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        var inputPath = await FfmpegTempHelper.WriteStreamToTempFileAsync(stream, ".tmp", ct).ConfigureAwait(false);
        try {
            return await ProbeAsync(inputPath, ct).ConfigureAwait(false);
        }
        finally {
            FfmpegTempHelper.TryDelete(inputPath);
        }
    }

    /// <inheritdoc />
    public async Task<Result<AudioProbeResult>> ProbeBytesAsync(byte[] bytes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(bytes, nameof(bytes));
        var inputPath = await FfmpegTempHelper.WriteBytesToTempFileAsync(bytes, ".tmp", ct).ConfigureAwait(false);
        try {
            return await ProbeAsync(inputPath, ct).ConfigureAwait(false);
        }
        finally {
            FfmpegTempHelper.TryDelete(inputPath);
        }
    }

    private async Task<Result<AudioProbeResult>> ProbeCoreAsync(string filePath, CancellationToken ct)
    {
        var ffprobePath = _runner.GetFfprobePath();
        var args = $"-v quiet -of json -show_format -show_streams \"{filePath.Replace("\"", "\\\"")}\"";
        _logger.LogDebug("Probing: {FfprobePath} {Args}", ffprobePath, args);
        var (exitCode, stdout, stderr) = await _runner.RunAsync(ffprobePath, args, ct).ConfigureAwait(false);
        if (exitCode != 0) {
            _logger.LogWarning("FFprobe failed with exit code {ExitCode}: {Stderr}", exitCode, stderr);
            return Result<AudioProbeResult>.Failure($"FFprobe failed: {stderr}", "FFprobeError");
        }

        return ParseProbeOutput(filePath, stdout);
    }

    private static Result<AudioProbeResult> ParseProbeOutput(string filePath, string json)
    {
        try {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var result = new AudioProbeResult { FilePath = filePath };
            if (root.TryGetProperty("format", out var formatEl)) {
                result.DurationSeconds = GetDouble(formatEl, "duration");
                result.Format = GetString(formatEl, "format_name");
                result.BitRate = GetLong(formatEl, "bit_rate");
                result.FileSizeBytes = GetLong(formatEl, "size");
            }

            if (root.TryGetProperty("streams", out var streamsEl) && streamsEl.ValueKind == JsonValueKind.Array) {
                foreach (var stream in streamsEl.EnumerateArray()) {
                    var codecType = GetString(stream, "codec_type");
                    if (codecType == "video")
                        result.HasVideo = true;
                    else if (codecType == "audio") {
                        result.HasAudio = true;
                        result.Codec = GetString(stream, "codec_name");
                        result.SampleRate = GetInt(stream, "sample_rate");
                        result.Channels = GetInt(stream, "channels");
                        if (result.Channels == null && stream.TryGetProperty("channel_layout", out var cl))
                            result.Channels = ParseChannelCount(GetString(cl));
                    }
                }
            }

            return Result<AudioProbeResult>.Success(result);
        }
        catch (JsonException ex) {
            return Result<AudioProbeResult>.Failure(ex, "ProbeParseError");
        }
    }

    private static double? GetDouble(JsonElement el, string name) => el.TryGetProperty(name, out var p) && p.TryGetDouble(out var v) ? v : null;

    private static long? GetLong(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;

        if (p.TryGetInt64(out var v))
            return v;

        if (p.ValueKind == JsonValueKind.String && long.TryParse(p.GetString(), out var parsed))
            return parsed;

        return null;
    }

    private static int? GetInt(JsonElement el, string name) => el.TryGetProperty(name, out var p) && p.TryGetInt32(out var v) ? v : null;

    private static string? GetString(JsonElement el, string name) => el.TryGetProperty(name, out var p) ? p.GetString() : null;

    private static string? GetString(JsonElement el) => el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static int? ParseChannelCount(string? layout)
    {
        if (string.IsNullOrWhiteSpace(layout))
            return null;

        return layout.ToLowerInvariant() switch {
            "mono" => 1,
            "stereo" => 2,
            "2.1" => 3,
            "3.0" or "2.1" => 3,
            "4.0" => 4,
            "5.0" => 5,
            "5.1" => 6,
            "6.1" => 7,
            "7.1" => 8,
            var _ when int.TryParse(layout, out var n) => n,
            var _ => null
        };
    }
}