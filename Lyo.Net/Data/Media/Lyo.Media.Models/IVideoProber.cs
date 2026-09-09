using Lyo.Result;

namespace Lyo.Media.Models;

/// <summary>
/// Reads metadata from a media file and flattens the first real video stream. Album art (<c>attached_pic</c>) does not count as video.
/// Muxed files still list every stream.
/// </summary>
public interface IVideoProber
{
    /// <summary>Probes a file or URL and returns duration, codecs, size, and related fields.</summary>
    Task<Result<MediaProbeResult>> ProbeAsync(string filePath, CancellationToken ct = default);

    /// <summary>Probes a stream. Non-seekable mp4 (moov at end) may still be staged to a temp file.</summary>
    Task<Result<MediaProbeResult>> ProbeStreamAsync(Stream stream, CancellationToken ct = default);

    /// <summary>Probes a byte array.</summary>
    Task<Result<MediaProbeResult>> ProbeBytesAsync(byte[] bytes, CancellationToken ct = default);
}
