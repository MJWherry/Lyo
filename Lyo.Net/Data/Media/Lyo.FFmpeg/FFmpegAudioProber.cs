using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FFmpeg;

/// <summary><see cref="IAudioProber" /> that reads metadata through ffprobe. Concurrent probes on one instance are safe.</summary>
public sealed class FFmpegAudioProber : IAudioProber
{
    private readonly FFmpegProbeEngine _engine;

    /// <summary>Creates a prober.</summary>
    public FFmpegAudioProber(FFmpegOptions? options = null, ILogger<FFmpegAudioProber>? logger = null, IMetrics? metrics = null)
        => _engine = new(options ?? new FFmpegOptions(), logger ?? NullLogger<FFmpegAudioProber>.Instance, metrics);

    /// <inheritdoc />
    public Task<Result<MediaProbeResult>> ProbeAsync(string filePath, CancellationToken ct = default) => _engine.ProbeAsync(filePath, ct);

    /// <inheritdoc />
    public Task<Result<MediaProbeResult>> ProbeStreamAsync(Stream stream, CancellationToken ct = default) => _engine.ProbeStreamAsync(stream, ct);

    /// <inheritdoc />
    public Task<Result<MediaProbeResult>> ProbeBytesAsync(byte[] bytes, CancellationToken ct = default) => _engine.ProbeBytesAsync(bytes, ct);
}
