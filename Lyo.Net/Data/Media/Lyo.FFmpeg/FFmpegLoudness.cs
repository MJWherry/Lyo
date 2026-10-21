using System.Globalization;
using System.Text.Json;
using Lyo.Media.Models;

namespace Lyo.FFmpeg;

/// <summary>Reads loudnorm JSON out of ffmpeg stderr and builds the second-pass filter.</summary>
internal static class FFmpegLoudness
{
    public static bool TryParse(string stderr, out Measurement measured)
    {
        measured = default;
        if (string.IsNullOrWhiteSpace(stderr))
            return false;

        var start = stderr.IndexOf('{');
        var end = stderr.LastIndexOf('}');
        if (start < 0 || end <= start)
            return false;

        try {
            using var doc = JsonDocument.Parse(stderr[start..(end + 1)]);
            var root = doc.RootElement;
            if (!TryNumber(root, "input_i", out var integrated) || !TryNumber(root, "input_tp", out var truePeak) || !TryNumber(root, "input_lra", out var range) ||
                !TryNumber(root, "input_thresh", out var threshold))
                return false;

            measured = new(integrated, truePeak, range, threshold);
            return true;
        }
        catch (JsonException) {
            return false;
        }
    }

    public static string FormatFilter(LoudnessTarget target, Measurement measured)
    {
        var culture = CultureInfo.InvariantCulture;
        return "loudnorm=" + $"I={target.IntegratedLufs.ToString("G", culture)}:" + $"TP={target.TruePeak.ToString("G", culture)}:" +
            $"LRA={target.Range.ToString("G", culture)}:" + $"measured_I={measured.Integrated}:" + $"measured_TP={measured.TruePeak}:" + $"measured_LRA={measured.Range}:" +
            $"measured_thresh={measured.Threshold}:" + "linear=true";
    }

    private static bool TryNumber(JsonElement root, string name, out string value)
    {
        value = "";
        if (!root.TryGetProperty(name, out var property))
            return false;

        value = property.ValueKind == JsonValueKind.String ? property.GetString() ?? "" : property.ToString();
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number);
    }

    public readonly record struct Measurement(string Integrated, string TruePeak, string Range, string Threshold);
}