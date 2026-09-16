using Lyo.Common.Core.Records;
using Lyo.Media.Models;

namespace Lyo.FFmpeg;

/// <summary>Builds one <c>-vf</c> value from scale, a caller filter, and subtitle burn-in.</summary>
internal static class FFmpegFilterChain
{
    public static string? Video(ScaleMode scale, int? width, int? height, string? videoFilter, SubtitleMode subtitles, string? subtitlePath)
    {
        var parts = new List<string>();
        var scaleFilter = Scale(scale, width, height);
        if (scaleFilter != null)
            parts.Add(scaleFilter);
        if (!string.IsNullOrWhiteSpace(videoFilter))
            parts.Add(videoFilter);
        if (subtitles == SubtitleMode.BurnIn && !string.IsNullOrWhiteSpace(subtitlePath))
            parts.Add("subtitles=" + EscapeSubtitlePath(subtitlePath));

        return parts.Count == 0 ? null : string.Join(",", parts);
    }

    public static string EscapeSubtitlePath(string path)
    {
        var escaped = new System.Text.StringBuilder(path.Length);
        foreach (var c in path) {
            if (c is '\\' or ':' or '\'' or ',')
                escaped.Append('\\');
            escaped.Append(c);
        }

        return escaped.ToString();
    }

    public static SubtitleCodec CodecFor(MediaContainer? format, SubtitleCodec? chosen)
    {
        if (chosen != null)
            return chosen;
        if (format == null)
            return SubtitleCodec.MovText;
        if (format == MediaContainer.WebM)
            return SubtitleCodec.WebVtt;
        if (string.Equals(format.Format, "matroska", StringComparison.OrdinalIgnoreCase))
            return SubtitleCodec.Ass;

        return SubtitleCodec.MovText;
    }

    private static string? Scale(ScaleMode scale, int? width, int? height)
    {
        if (scale == ScaleMode.Stretch || (width == null && height == null))
            return null;
        if (scale == ScaleMode.Fit && width is int w && height is int h)
            return $"scale={w}:{h}:force_original_aspect_ratio=decrease";
        if (scale == ScaleMode.Fit && width is int onlyWidth)
            return $"scale={onlyWidth}:-2";
        if (scale == ScaleMode.Fit && height is int onlyHeight)
            return $"scale=-2:{onlyHeight}";
        if (scale == ScaleMode.Fill && width is int fillW && height is int fillH)
            return $"scale={fillW}:{fillH}:force_original_aspect_ratio=increase,crop={fillW}:{fillH}";

        return null;
    }
}
