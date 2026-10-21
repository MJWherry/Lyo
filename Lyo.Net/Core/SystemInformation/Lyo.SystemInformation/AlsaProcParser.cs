using System.Globalization;
using System.Text.RegularExpressions;

namespace Lyo.SystemInformation;

/// <summary>Parses <c>/proc/asound/cards</c> and <c>/proc/asound/pcm</c> into audio endpoints. Form factor and the default device stay null.</summary>
internal static class AlsaProcParser
{
    private static readonly Regex CardHeaderRegex = new(@"^\s*(\d+)\s+\[[^\]]+\]:\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex PcmLineRegex = new(@"^(?<card>\d+)-(?<dev>\d+):\s*(?<rest>.*)$", RegexOptions.Compiled);
    private static readonly Regex PlaybackRegex = new(@"playback\s+(\d+)", RegexOptions.Compiled);
    private static readonly Regex CaptureRegex = new(@"capture\s+(\d+)", RegexOptions.Compiled);

    /// <summary>Builds one endpoint per PCM line that has a playback or capture count. Card long names fill <see cref="AudioDeviceInfo.Description" />.</summary>
    internal static IReadOnlyList<AudioDeviceInfo> Parse(string? cards, string? pcm)
    {
        if (pcm is null || string.IsNullOrWhiteSpace(pcm))
            return [];

        var descriptions = ReadCardDescriptions(cards);
        var devices = new List<AudioDeviceInfo>();
        foreach (var raw in pcm.Split('\n')) {
            var device = ParsePcmLine(raw.Trim(), descriptions);
            if (device is not null)
                devices.Add(device);
        }

        return devices;
    }

    private static Dictionary<int, string> ReadCardDescriptions(string? cards)
    {
        var descriptions = new Dictionary<int, string>();
        if (cards is null || string.IsNullOrWhiteSpace(cards))
            return descriptions;

        int? current = null;
        foreach (var raw in cards.Split('\n')) {
            var header = CardHeaderRegex.Match(raw);
            if (header.Success) {
                current = int.Parse(header.Groups[1].Value, CultureInfo.InvariantCulture);
                var rest = header.Groups[2].Value;
                var separator = rest.IndexOf(" - ", StringComparison.Ordinal);
                descriptions[current.Value] = (separator >= 0 ? rest.Substring(separator + 3) : rest).Trim();
                continue;
            }

            if (current is int card && raw.Length > 0 && char.IsWhiteSpace(raw[0])) {
                var longName = raw.Trim();
                if (longName.Length > 0)
                    descriptions[card] = longName;
            }
        }

        return descriptions;
    }

    private static AudioDeviceInfo? ParsePcmLine(string line, Dictionary<int, string> descriptions)
    {
        if (line.Length == 0)
            return null;

        var match = PcmLineRegex.Match(line);
        if (!match.Success)
            return null;

        var rest = match.Groups["rest"].Value;
        var playback = PlaybackRegex.Match(rest);
        var capture = CaptureRegex.Match(rest);
        var playCount = playback.Success ? int.Parse(playback.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        var captureCount = capture.Success ? int.Parse(capture.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        if (playCount == 0 && captureCount == 0)
            return null;

        var cutAt = rest.Length;
        if (playback.Success)
            cutAt = Math.Min(cutAt, playback.Index);

        if (capture.Success)
            cutAt = Math.Min(cutAt, capture.Index);

        var head = rest.Substring(0, cutAt).Trim().TrimEnd(':').Trim();
        var colon = head.IndexOf(':');
        var name = (colon >= 0 ? head.Substring(0, colon) : head).Trim();
        if (name.Length == 0)
            return null;

        var kind = playCount > 0 && captureCount > 0 ? AudioDeviceInfo.Duplex : playCount > 0 ? AudioDeviceInfo.Playback : AudioDeviceInfo.Capture;
        descriptions.TryGetValue(int.Parse(match.Groups["card"].Value, CultureInfo.InvariantCulture), out var description);
        return new(name, kind, description, null, null);
    }
}