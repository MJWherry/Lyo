using Lyo.Exceptions;
using Lyo.Media.Models;

namespace Lyo.FFmpeg;

/// <summary>
/// Maps generic codec ids onto ffmpeg encoder names. Unknown ids pass through so <c>Custom("libx265")</c> still works.
/// </summary>
internal static class FFmpegCodecMap
{
    public static string ToFfmpegAudio(string id) => MapAudio(Normalize(id));

    public static string ToFfmpegVideo(string id) => ToFfmpegVideo(id, null);

    public static string ToFfmpegVideo(string id, HardwareAccel? hardware)
    {
        var codec = Normalize(id);
        if (hardware == null || hardware == HardwareAccel.None)
            return MapVideo(codec);

        if (codec == "mpeg4")
            throw new ArgumentException($"MPEG-4 Part 2 has no {hardware.Name} encoder.", nameof(hardware));

        var mapped = hardware.Id switch {
            "nvenc" => Nvenc(codec),
            "qsv" => Qsv(codec),
            "vaapi" => Vaapi(codec),
            "videotoolbox" => VideoToolbox(codec),
            _ => null
        };
        if (mapped == null)
            throw new ArgumentException($"{hardware.Name} has no encoder for '{codec}'.", nameof(hardware));

        return mapped;
    }

    public static string HwaccelFlag(HardwareAccel hardware)
        => hardware.Id switch {
            "nvenc" => "cuda",
            "qsv" => "qsv",
            "vaapi" => "vaapi",
            "videotoolbox" => "videotoolbox",
            _ => hardware.Id
        };

    public static string FromFfmpegAudio(string encoderName) => UnmapAudio(Normalize(encoderName));

    public static string FromFfmpegVideo(string encoderName) => UnmapVideo(Normalize(encoderName));

    private static string? Nvenc(string codec)
        => codec switch {
            "h264" => "h264_nvenc",
            "hevc" => "hevc_nvenc",
            "av1" => "av1_nvenc",
            _ => null
        };

    private static string? Qsv(string codec)
        => codec switch {
            "h264" => "h264_qsv",
            "hevc" => "hevc_qsv",
            "av1" => "av1_qsv",
            "vp9" => "vp9_qsv",
            _ => null
        };

    private static string? Vaapi(string codec)
        => codec switch {
            "h264" => "h264_vaapi",
            "hevc" => "hevc_vaapi",
            "vp8" => "vp8_vaapi",
            "vp9" => "vp9_vaapi",
            _ => null
        };

    private static string? VideoToolbox(string codec)
        => codec switch {
            "h264" => "h264_videotoolbox",
            "hevc" => "hevc_videotoolbox",
            _ => null
        };

    private static string MapAudio(string id)
        => id switch {
            "mp3" => "libmp3lame",
            "opus" => "libopus",
            "vorbis" => "libvorbis",
            var key => key
        };

    private static string MapVideo(string id)
        => id switch {
            "h264" => "libx264",
            "vp9" => "libvpx-vp9",
            "vp8" => "libvpx",
            "hevc" => "libx265",
            "av1" => "libsvtav1",
            var key => key
        };

    private static string UnmapAudio(string encoderName)
        => encoderName switch {
            "libmp3lame" => "mp3",
            "libopus" => "opus",
            "libvorbis" => "vorbis",
            var key => key
        };

    private static string UnmapVideo(string encoderName)
        => encoderName switch {
            "libx264" => "h264",
            "libvpx-vp9" => "vp9",
            "libvpx" => "vp8",
            "libx265" => "hevc",
            "libsvtav1" => "av1",
            var key => key
        };

    private static string Normalize(string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().TrimStart('.').ToLowerInvariant();
    }
}
