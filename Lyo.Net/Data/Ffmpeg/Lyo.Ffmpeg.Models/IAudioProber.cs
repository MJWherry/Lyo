using Lyo.Common;

namespace Lyo.Ffmpeg.Models;

/// <summary>Service interface for probing audio/video files to extract metadata.</summary>
public interface IAudioProber
{
    /// <summary>Probes a file and returns metadata (duration, format, codec, etc.).</summary>
    Task<Result<AudioProbeResult>> ProbeAsync(string filePath, CancellationToken ct = default);

    /// <summary>Probes a stream and returns metadata.</summary>
    Task<Result<AudioProbeResult>> ProbeStreamAsync(Stream stream, CancellationToken ct = default);

    /// <summary>Probes a byte array and returns metadata.</summary>
    Task<Result<AudioProbeResult>> ProbeBytesAsync(byte[] bytes, CancellationToken ct = default);
}