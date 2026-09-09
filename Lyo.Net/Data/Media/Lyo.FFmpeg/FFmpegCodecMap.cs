using Lyo.Exceptions;

namespace Lyo.FFmpeg;

/// <summary>
/// Maps generic codec ids onto ffmpeg encoder names. Unknown ids pass through so <c>Custom("libx265")</c> still works.
/// </summary>
internal static class FFmpegCodecMap
{
    public static string ToFfmpegAudio(string id) => MapAudio(Normalize(id));

    public static string ToFfmpegVideo(string id) => MapVideo(Normalize(id));

    public static string FromFfmpegAudio(string encoderName) => UnmapAudio(Normalize(encoderName));

    public static string FromFfmpegVideo(string encoderName) => UnmapVideo(Normalize(encoderName));

    private static string MapAudio(string id)
        => id switch {
            "mp3" => "libmp3lame",
            "opus" => "libopus",
            var key => key
        };

    private static string MapVideo(string id)
        => id switch {
            "h264" => "libx264",
            "vp9" => "libvpx-vp9",
            var key => key
        };

    private static string UnmapAudio(string encoderName)
        => encoderName switch {
            "libmp3lame" => "mp3",
            "libopus" => "opus",
            var key => key
        };

    private static string UnmapVideo(string encoderName)
        => encoderName switch {
            "libx264" => "h264",
            "libvpx-vp9" => "vp9",
            var key => key
        };

    private static string Normalize(string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().TrimStart('.').ToLowerInvariant();
    }
}
