using System.Text.Json;
using Lyo.Common.Core.Conversion;
using Lyo.Media.Models;
using Lyo.Result;

namespace Lyo.FFmpeg;

/// <summary>Parses ffprobe JSON into <see cref="MediaProbeResult" />. Uses <see cref="TypeConversion" /> for field values.</summary>
internal static class FfprobeJsonParser
{
    public static Result<MediaProbeResult> Parse(string? filePath, string json)
    {
        try {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            double? durationSeconds = null;
            string? format = null;
            long? bitRate = null;
            long? fileSizeBytes = null;
            IReadOnlyDictionary<string, string>? rawMetadata = null;
            if (root.TryGetProperty("format", out var formatEl)) {
                durationSeconds = Read<double>(formatEl, "duration");
                format = ReadString(formatEl, "format_name");
                bitRate = Read<long>(formatEl, "bit_rate");
                fileSizeBytes = Read<long>(formatEl, "size");
                rawMetadata = ReadTags(formatEl);
            }

            IReadOnlyList<MediaStreamInfo> streams = [];
            if (root.TryGetProperty("streams", out var streamsEl) && streamsEl.ValueKind == JsonValueKind.Array) {
                var list = new List<MediaStreamInfo>();
                foreach (var stream in streamsEl.EnumerateArray())
                    list.Add(ParseStream(stream));

                streams = list;
            }

            var audio = streams.FirstOrDefault(s => string.Equals(s.CodecType, "audio", StringComparison.OrdinalIgnoreCase));
            var video = streams.FirstOrDefault(s => string.Equals(s.CodecType, "video", StringComparison.OrdinalIgnoreCase) && !s.IsAttachedPicture);
            return Result<MediaProbeResult>.Success(
                new() {
                    FilePath = filePath,
                    DurationSeconds = durationSeconds,
                    Format = format,
                    BitRate = bitRate,
                    FileSizeBytes = fileSizeBytes,
                    RawMetadata = rawMetadata,
                    Streams = streams,
                    HasAudio = audio != null,
                    Codec = audio?.CodecName,
                    SampleRate = audio?.SampleRate,
                    Channels = audio?.Channels,
                    HasVideo = video != null,
                    VideoCodec = video?.CodecName,
                    Width = video?.Width,
                    Height = video?.Height,
                    Fps = video?.Fps
                });
        }
        catch (JsonException ex) {
            return Result<MediaProbeResult>.Failure(ex, Constants.Errors.ProbeParseError);
        }
    }

    internal static bool TryParseRational(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var slash = text.IndexOf('/');
        if (slash > 0) {
            var left = text[..slash];
            var right = text[(slash + 1)..];
            if (!TypeConversion.TryConvertTo<double>(left, out var n) || !TypeConversion.TryConvertTo<double>(right, out var d) || d == 0)
                return false;
            if (n == 0)
                return false;

            value = n / d;
            return true;
        }

        if (!TypeConversion.TryConvertTo<double>(text, out var parsed) || parsed <= 0)
            return false;

        value = parsed;
        return true;
    }

    private static MediaStreamInfo ParseStream(JsonElement stream)
    {
        var channels = Read<int>(stream, "channels") ?? ParseChannelLayout(ReadString(stream, "channel_layout"));
        var rate = ReadString(stream, "avg_frame_rate");
        if (!TryParseRational(rate, out var fps))
            TryParseRational(ReadString(stream, "r_frame_rate"), out fps);

        var attachedPic = false;
        if (stream.TryGetProperty("disposition", out var disp) && disp.TryGetProperty("attached_pic", out var pic))
            attachedPic = TypeConversion.TryConvertTo<int>(pic, out var flag) && flag == 1;

        return new() {
            Index = Read<int>(stream, "index") ?? 0,
            CodecType = ReadString(stream, "codec_type"),
            CodecName = ReadString(stream, "codec_name"),
            SampleRate = Read<int>(stream, "sample_rate"),
            Channels = channels,
            BitRate = Read<long>(stream, "bit_rate"),
            Width = Read<int>(stream, "width"),
            Height = Read<int>(stream, "height"),
            PixelFormat = ReadString(stream, "pix_fmt"),
            Fps = fps > 0 ? fps : null,
            IsAttachedPicture = attachedPic
        };
    }

    private static IReadOnlyDictionary<string, string>? ReadTags(JsonElement formatEl)
    {
        if (!formatEl.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Object)
            return null;

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in tags.EnumerateObject()) {
            if (TypeConversion.TryConvertTo<string>(prop.Value, out var value) && value != null)
                dict[prop.Name] = value;
        }

        return dict.Count == 0 ? null : dict;
    }

    private static T? Read<T>(JsonElement parent, string name)
        where T : struct
    {
        if (!parent.TryGetProperty(name, out var p))
            return null;

        return TypeConversion.TryConvertTo<T>(p, out var value) ? value : null;
    }

    private static string? ReadString(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var p))
            return null;

        return TypeConversion.TryConvertTo<string>(p, out var value) ? value : null;
    }

    private static int? ParseChannelLayout(string? layout)
        => layout?.ToLowerInvariant() switch {
            "mono" => 1,
            "stereo" => 2,
            "3.0" or "2.1" => 3,
            "4.0" => 4,
            "5.0" => 5,
            "5.1" => 6,
            "6.1" => 7,
            "7.1" => 8,
            var _ when TypeConversion.TryConvertTo<int>(layout, out var n) => n,
            var _ => null
        };
}
